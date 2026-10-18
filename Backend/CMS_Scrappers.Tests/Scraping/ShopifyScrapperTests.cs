using System.Net;
using System.Text;
using CMS_Scrappers.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

namespace CMS_Scrappers.Tests.Scraping;

/// <summary>
/// Drives the real ShoipfyScrapper against a tiny fake Shopify on localhost that behaves like a
/// password-protected store: products.json answers 401 until the storefront_digest cookie is present,
/// POST /password sets that cookie, then products.json serves one page of products.
/// </summary>
public class ShopifyScrapperTests
{
    private const string Password = "letmein";

    private static ShoipfyScrapper NewScrapper() =>
        new(new NoProxies(), NullLogger<ShoipfyScrapper>.Instance);

    [Fact]
    public async Task Password_protected_store_logs_in_then_returns_the_page()
    {
        using var store = FakeShopify.Start(requirePassword: true);

        var result = await NewScrapper().Getproducts(store.BaseUrl, Password, maxPages: 1);

        Assert.Single(result.Pages);
        Assert.Equal(2, result.Pages[0].Products.Count);
        Assert.Equal("supreme-box-logo-hoodie", result.Pages[0].Products[0].Handle);
        Assert.Equal(1, store.LoginAttempts);
        Assert.True(store.ProductsRequestsWithCookie >= 1, "products.json must be re-requested with the session cookie");
    }

    [Fact]
    public async Task Public_store_never_touches_the_password_page()
    {
        using var store = FakeShopify.Start(requirePassword: false);

        var result = await NewScrapper().Getproducts(store.BaseUrl, storefrontPassword: null, maxPages: 1);

        Assert.Single(result.Pages);
        Assert.Equal(0, store.LoginAttempts);
    }

    [Fact]
    public async Task Wrong_password_yields_no_pages_and_stops_retrying()
    {
        using var store = FakeShopify.Start(requirePassword: true);

        var result = await NewScrapper().Getproducts(store.BaseUrl, "wrong-password", maxPages: 1);

        Assert.Empty(result.Pages);
        // 2 login attempts per page max, then the proxy-switch path (6 switches), each switch allowing 2 more tries
        Assert.InRange(store.LoginAttempts, 1, 2 * 7);
    }

    /// <summary>
    /// Opt-in live check against the real supplier. Set CMS_TEST_STORE_URL and (if needed)
    /// CMS_TEST_STORE_PASSWORD, then run: dotnet test --filter Live_store. Fetches page 1 only.
    /// Without the env vars it does nothing, so CI stays offline.
    /// </summary>
    [Fact]
    public async Task Live_store_first_page_is_reachable()
    {
        var url = Environment.GetEnvironmentVariable("CMS_TEST_STORE_URL");
        if (string.IsNullOrWhiteSpace(url)) return;
        var password = Environment.GetEnvironmentVariable("CMS_TEST_STORE_PASSWORD");

        var result = await NewScrapper().Getproducts(url, password, maxPages: 1);

        Assert.Single(result.Pages);
        Assert.NotEmpty(result.Pages[0].Products);
    }

    private sealed class NoProxies : IProxyManager
    {
        public bool HasProxies => false;
        public WebProxy? GetNextProxy() => null;
        public Task RefreshProxiesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Minimal Shopify storefront on HttpListener. One page of two products, then an empty page.</summary>
    private sealed class FakeShopify : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly bool _requirePassword;
        private readonly CancellationTokenSource _cts = new();

        public string BaseUrl { get; }
        public int LoginAttempts;
        public int ProductsRequestsWithCookie;

        private const string PageOne = """
            {"products":[
              {"id":1,"title":"Supreme Box Logo Hoodie","handle":"supreme-box-logo-hoodie","body_html":"<p>New</p>","vendor":"Supreme","product_type":"Hoodie","tags":["New"],
               "variants":[{"id":11,"title":"Medium","option1":"Medium","sku":"SUP-1","available":true,"price":"500.00","position":1}],
               "images":[{"id":111,"position":1,"src":"https://cdn/1.jpg","width":1,"height":1,"product_id":1,"variant_ids":[]}],
               "options":[{"name":"Size","position":1,"values":["Medium"]}],
               "published_at":"2026-01-01T00:00:00Z","created_at":"2026-01-01T00:00:00Z","updated_at":"2026-01-01T00:00:00Z"},
              {"id":2,"title":"Chrome Hearts Cap","handle":"chrome-hearts-cap","body_html":"","vendor":"Chrome Hearts","product_type":"Hat","tags":["Used"],
               "variants":[{"id":22,"title":"OS","option1":"OS","sku":"CH-1","available":true,"price":"900.00","position":1}],
               "images":[],"options":[{"name":"Size","position":1,"values":["OS"]}],
               "published_at":"2026-01-01T00:00:00Z","created_at":"2026-01-01T00:00:00Z","updated_at":"2026-01-01T00:00:00Z"}
            ]}
            """;

        private FakeShopify(bool requirePassword)
        {
            _requirePassword = requirePassword;
            var port = FreePort();
            BaseUrl = $"http://localhost:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public static FakeShopify Start(bool requirePassword) => new(requirePassword);

        private async Task ServeAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { return; }

                var req = ctx.Request;
                var res = ctx.Response;
                var hasSession = req.Cookies["storefront_digest"] != null;

                if (req.HttpMethod == "POST" && req.Url!.AbsolutePath == "/password")
                {
                    Interlocked.Increment(ref LoginAttempts);
                    using var reader = new StreamReader(req.InputStream);
                    var form = await reader.ReadToEndAsync();
                    if (form.Contains($"password={Password}"))
                    {
                        res.Headers.Add("Set-Cookie", "storefront_digest=abc123; Path=/");
                        res.StatusCode = 302;
                        res.RedirectLocation = "/";
                    }
                    else
                    {
                        res.StatusCode = 200;           // Shopify re-renders the form on a wrong password
                        await WriteAsync(res, "<html>password page</html>", "text/html");
                        continue;
                    }
                    res.Close();
                    continue;
                }

                if (req.Url!.AbsolutePath == "/products.json")
                {
                    if (_requirePassword && !hasSession)
                    {
                        res.StatusCode = 401;
                        await WriteAsync(res, "Unauthorized", "text/plain");
                        continue;
                    }
                    if (hasSession) Interlocked.Increment(ref ProductsRequestsWithCookie);

                    var page = req.QueryString["page"] ?? "1";
                    await WriteAsync(res, page == "1" ? PageOne : "{\"products\":[]}", "application/json");
                    continue;
                }

                res.StatusCode = 404;
                res.Close();
            }
        }

        private static async Task WriteAsync(HttpListenerResponse res, string body, string contentType)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            res.ContentType = contentType;
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        private static int FreePort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); _listener.Close(); } catch { }
        }
    }
}
