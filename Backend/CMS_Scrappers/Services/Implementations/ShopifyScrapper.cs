using System.Net;
using System.Net.Http.Json;
using CMS_Scrappers.Services.Interfaces;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public class ShoipfyScrapper : Scrap_shopify
{
    private readonly IProxyManager _proxyManager;

    public ShoipfyScrapper(IProxyManager proxyManager)
    {
        _proxyManager = proxyManager;
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
        using var httpClient = CreateClientForRequest();
        
        var response = new ShopifyGetAllProductsResponse();
        var pageNumber = 1;
        var baseUrl = url.TrimEnd('/');

        while (true)
        {
            var requestUrl = $"{baseUrl}/products.json?limit=250&page={pageNumber}";
            var httpResponse = await httpClient.GetAsync(requestUrl);

            if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = httpResponse.Headers.RetryAfter?.Delta?.Seconds ?? 60;
                await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                continue;
            }

            if (!httpResponse.IsSuccessStatusCode) break;

            var productsResponse = await httpResponse.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();
            if (productsResponse?.Products == null || productsResponse.Products.Count == 0) break;

            response.Pages.Add(productsResponse);
            pageNumber++;

            await Task.Delay(6000);
        }

        return response;
    }
}