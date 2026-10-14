using System.Net;
using System.Net.Http.Json;
using CMS_Scrappers.Services.Interfaces;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public class ShoipfyScrapper : Scrap_shopify
{
    private readonly IProxyManager _proxyManager;
    private readonly ILogger<ShoipfyScrapper> _logger;

    // How many different proxies to try before giving up on a run
    private const int MaxProxySwitches = 6;

    public ShoipfyScrapper(IProxyManager proxyManager, ILogger<ShoipfyScrapper> logger)
    {
        _proxyManager = proxyManager;
        _logger = logger;
    }

    private HttpClient CreateClientForRequest()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        // Pull next proxy abstraction
        var proxy = _proxyManager.GetNextProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

        return client;
    }

    public async Task<ShopifyGetAllProductsResponse> Getproducts(string url)
    {
        var httpClient = CreateClientForRequest();
        var response = new ShopifyGetAllProductsResponse();
        var pageNumber = 1;
        var baseUrl = url.TrimEnd('/');
        var proxySwitches = 0;

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
            httpClient = CreateClientForRequest();
            await Task.Delay(2000);
            return true;
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
                    // A 200 that is not product JSON is a bot-challenge page
                    if (await SwitchProxyAsync("non-JSON body: " + ex.Message)) continue;
                    break;
                }

                if (productsResponse?.Products == null || productsResponse.Products.Count == 0) break;

                response.Pages.Add(productsResponse);
                pageNumber++;

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
}