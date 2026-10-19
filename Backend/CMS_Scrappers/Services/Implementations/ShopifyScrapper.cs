using System.Net;
using System.Net.Http.Json;
using CMS_Scrappers.Services.Interfaces;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public class ShoipfyScrapper : Scrap_shopify
{
    private readonly IProxyManager _proxyManager;
    private readonly ILogger<ShoipfyScrapper> _logger;

  
    private const int MaxProxySwitches = 6;

    
    private const int MaxLoginsPerPage = 2;

  

    public ShoipfyScrapper(IProxyManager proxyManager, ILogger<ShoipfyScrapper> logger)
    {
        _proxyManager = proxyManager;
        _logger = logger;
    }

    /// <param name="cookies">
    /// Shared across proxy switches so a storefront login survives them. Null keeps the
    /// pre-password behaviour: no cookie jar and redirects followed automatically.
    /// </param>
    private HttpClient CreateClientForRequest(CookieContainer? cookies)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        };

        if (cookies != null)
        {
            handler.UseCookies = true;
            handler.CookieContainer = cookies;
            // We must see the 302 to /password ourselves instead of silently landing on the login page
            handler.AllowAutoRedirect = false;
        }

        // Pull next proxy abstraction
        var proxy = _proxyManager.GetNextProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0");

        return client;
    }

    public async Task<ShopifyGetAllProductsResponse> Getproducts(string url, string? storefrontPassword = null, int? maxPages = null)
    {
        var baseUrl = url.TrimEnd('/');
        var hasPassword = !string.IsNullOrWhiteSpace(storefrontPassword);
        var cookies = hasPassword ? new CookieContainer() : null;

        var httpClient = CreateClientForRequest(cookies);
        var response = new ShopifyGetAllProductsResponse();
        var pageNumber = 1;
        var proxySwitches = 0;
        var loginsThisPage = 0;
        var redirectsFollowed = 0;

        // The supplier bans individual proxy IPs (403 block page); a failed page is retried through the next proxy
        // instead of silently ending the scrape with an empty catalogue.
        async Task<bool> SwitchProxyAsync(string reason)
        {
            if (proxySwitches >= MaxProxySwitches)
            {
                _logger.LogError("Supplier fetch gave up on page {Page} after trying {Count} proxies ({Reason}); {Pages} pages fetched",
                    pageNumber, proxySwitches + 1, reason, response.Pages.Count);
                return false;
            }
            proxySwitches++;
            _logger.LogWarning("Supplier fetch page {Page} failed ({Reason}); switching proxy {Switch}/{Max}",
                pageNumber, reason, proxySwitches, MaxProxySwitches);
            httpClient.Dispose();
            httpClient = CreateClientForRequest(cookies);
            await Task.Delay(2000);
            return true;
        }

        // Logs in to the storefront password page. Returns false when the store rejected the password
        // or the page cap is hit, so the caller can fall through to the proxy-switch path.
        async Task<bool> TryLoginAsync(string reason)
        {
            if (loginsThisPage >= MaxLoginsPerPage)
            {
                _logger.LogWarning("Storefront login already tried {Count}x on page {Page} ({Reason})", loginsThisPage, pageNumber, reason);
                return false;
            }
            loginsThisPage++;
            _logger.LogInformation("Storefront password required on page {Page} ({Reason}); logging in (attempt {Attempt})", pageNumber, reason, loginsThisPage);
            return await LoginToStorefrontAsync(httpClient, baseUrl, storefrontPassword!, cookies!);
        }

        try
        {
            while (true)
            {
                var requestUrl = $"{baseUrl}/products.json?limit=250&page={pageNumber}";
                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await httpClient.GetAsync(requestUrl);
                }
                catch (Exception ex)
                {
                    if (await SwitchProxyAsync(ex.GetType().Name + ": " + ex.Message)) continue;
                    break;
                }

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = httpResponse.Headers.RetryAfter?.Delta?.Seconds ?? 60;
                    _logger.LogWarning("Supplier rate-limited page {Page}; waiting {Seconds}s", pageNumber, retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                    continue;
                }

                // Password-protected store: Shopify answers 302 -> /password until the storefront_digest cookie is set
                if (hasPassword && NeedsStorefrontLogin(httpResponse))
                {
                    if (await TryLoginAsync($"HTTP {(int)httpResponse.StatusCode}")) continue;
                    if (await SwitchProxyAsync("storefront login failed")) continue;
                    break;
                }

                // Canonical-host redirect (e.g. bare domain -> www) is neither a password challenge nor a blocked proxy:
                // retry at the host the store names. Once per run.
                if (hasPassword && IsRedirect(httpResponse) && httpResponse.Headers.Location != null && redirectsFollowed < 1)
                {
                    var location = httpResponse.Headers.Location;
                    var target = location.IsAbsoluteUri ? location : new Uri(new Uri(baseUrl + "/"), location);
                    var newBase = $"{target.Scheme}://{target.Host}";
                    _logger.LogWarning("Supplier redirected {Old} -> {New}; following once. Set Scrappers.Baseurl to the new host to avoid this",
                        baseUrl, newBase);
                    baseUrl = newBase;
                    redirectsFollowed++;
                    continue;
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    if (await SwitchProxyAsync($"HTTP {(int)httpResponse.StatusCode}")) continue;
                    break;
                }

                ShopifyStoreProductsResponse? productsResponse;
                try
                {
                    productsResponse = await httpResponse.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();
                }
                catch (Exception ex)
                {
                    // A 200 that is not product JSON is either the password page (if we are not logged in yet)
                    // or a bot-challenge page
                    // Always try the login here when a password is set: the jar cannot tell us whether we are logged in
                    // (_shopify_essential is set on every response), and the per-page login cap prevents a loop.
                    if (hasPassword)
                    {
                        if (await TryLoginAsync("non-JSON body")) continue;
                    }
                    if (await SwitchProxyAsync("non-JSON body: " + ex.Message)) continue;
                    break;
                }

                if (productsResponse?.Products == null || productsResponse.Products.Count == 0) break;

                response.Pages.Add(productsResponse);
                pageNumber++;
                loginsThisPage = 0;

                if (maxPages.HasValue && response.Pages.Count >= maxPages.Value) break;

                await Task.Delay(3000);
            }
        }
        finally
        {
            httpClient.Dispose();
        }

        _logger.LogInformation("Supplier fetch finished: {Pages} pages, {Switches} proxy switches", response.Pages.Count, proxySwitches);
        return response;
    }

    // Shopify signals "password required" two ways: HTML paths get 302 -> /password, .json paths get a bare 401
    private static bool NeedsStorefrontLogin(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized) return true;

        var code = (int)response.StatusCode;
        if (code < 300 || code > 399) return false;
        var location = response.Headers.Location?.ToString() ?? "";
        return location.Contains("/password", StringComparison.OrdinalIgnoreCase);
    }

    // Checked by name across the whole jar, not by URL: Shopify sets the cookie for the bare domain
    // (vnds.world) while we request the www host, and a URL lookup misses it
    private static bool IsRedirect(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code >= 300 && code <= 399;
    }


    /// <summary>
    /// Posts the storefront password the same way the browser form does. Success is judged by the
    /// storefront_digest cookie appearing in the jar, not by the status code: Shopify answers 302 on
    /// success and re-renders the form with 200 on a wrong password.
    /// </summary>
    private async Task<bool> LoginToStorefrontAsync(HttpClient client, string baseUrl, string password, CookieContainer cookies)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["form_type"] = "storefront_password",
            ["utf8"] = "✓",
            ["password"] = password,
        });

        try
        {
            using var loginResponse = await client.PostAsync($"{baseUrl}/password", form);
            // Shopify answers a 3xx away from /password on success (and sets a session cookie); a wrong
            // password re-renders the form with 200. The legacy storefront_digest cookie is a fallback signal.
            var code = (int)loginResponse.StatusCode;
            var location = loginResponse.Headers.Location?.ToString() ?? "";
            var redirectedAway = code is >= 300 and <= 399 && !location.Contains("/password", StringComparison.OrdinalIgnoreCase);
            var legacyCookie = cookies.GetAllCookies().Any(c => c.Name == "storefront_digest");
            if (redirectedAway || legacyCookie)
            {
                _logger.LogInformation("Storefront login accepted for {Base} (HTTP {Status} -> {Location}, cookies [{Cookies}])",
                    baseUrl, code, location == "" ? "-" : location, string.Join(",", cookies.GetAllCookies().Select(c => c.Name)));
                return true;
            }

            var names = string.Join(",", cookies.GetAllCookies().Select(c => $"{c.Name}@{c.Domain}"));
            _logger.LogError("Storefront login rejected for {Base} (HTTP {Status}, Location {Location}, jar [{Cookies}]); check Suppliers__<Name>__StorefrontPassword",
                baseUrl, (int)loginResponse.StatusCode, loginResponse.Headers.Location?.ToString() ?? "-", names);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storefront login request failed for {Base}", baseUrl);
            return false;
        }
    }
}
