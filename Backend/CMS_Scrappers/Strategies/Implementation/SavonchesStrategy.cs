using System.Net;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CMS_Scrappers.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class SavonchesStrategy : IShopifyParsingStrategy
{
    private static readonly string Scrappername = "Savonches";
    private readonly ILogger<SavonchesStrategy> _looger;
    private readonly HttpClient _client;
    private readonly Scrap_shopify _Scrap_shopify;
    private readonly IScrapperRepository _scrapperRepository;
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IProxyManager _proxyManager;

    // Proxy client shared by the enrichment tasks of the current page; swapped for a fresh proxy on 429/403
    private HttpClient _enrichClient = null!;
    private readonly object _enrichClientLock = new();
    // The supplier's per-IP limit is low; move to the next proxy every few requests so no single IP ever reaches it
    private int _requestsOnCurrentClient;
    private const int RequestsPerProxy = 20;

    public SavonchesStrategy(
        Scrap_shopify scrap_Shopify,
        IHttpClientFactory clientFactory,
        IScrapperRepository scrapperRepository,
        ILogger<SavonchesStrategy> logger,
        AppDbContext context,
        IServiceProvider serviceProvider,
        IProxyManager proxyManager)
    {
        _looger = logger;
        _Scrap_shopify = scrap_Shopify;
        _scrapperRepository = scrapperRepository;
        _client = clientFactory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
        _context = context;
        _serviceProvider = serviceProvider;
        _proxyManager = proxyManager;
    }

    private static readonly string[] agents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Linux; Android 10; Pixel 3 XL) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0 Mobile Safari/537.36"
    };

    public async Task<List<ShopifyFlatProduct>> MapAndEnrichProductAsync(ShopifyGetAllProductsResponse rawProduct, string storeBaseUrl)
    {
        var allRawProducts = rawProduct.Pages.SelectMany(page => page.Products);
        lock (_enrichClientLock)
        {
            _enrichClient = GetProxyClient();
        }
        var initialProductList = allRawProducts.Select(product =>
        {
            
            var images = product.Images.Select(i => new ProductImageRecordDTO
            {
                Priority = i.Position,
                Url = i.Src
            }).ToList();

            var variants = product.Variants.Select(v => new ProductVariantRecordDTO
            {
                Size = v.Option1,
                SKU = v.Sku,
                Available = v.Available ? 1 : 0,
                Price = decimal.TryParse(v.Price, out var p) ? p : 0
            }).ToList();

            return new ShopifyFlatProduct
            {
                Id = product.Id,
                Title = product.Title,
                Handle = product.Handle,
                Images = images,
                Variants = variants,
                Category = product.ProductType,
                Status="raw",
                Price = decimal.TryParse(product.Variants.FirstOrDefault()?.Price, out var price) ? price : 0,
                Brand = product.Vendor,
                Gender = GetGender(product.Tags)
            };

        }).ToList();

        var sema = new SemaphoreSlim(3); 

        var enrichmentTasks = initialProductList.Select(async p =>
        {
            await sema.WaitAsync();
            try
            {
               
                var fullUrl = $"{storeBaseUrl}products/{p.Handle}";
                var skipResult = await ShouldSkip(fullUrl);
                if (!skipResult.ShouldSkip)
                {
                   // await Task.Delay(RandomDelay());
                    await Get_attributes(p, storeBaseUrl);
                }
                else
                {
                    //_looger.LogWarning($"rolling back from the {fullUrl}");
                    p.ProductUrl = fullUrl;
                    p.Description = skipResult.ExistingDescription; 
                    p.ScraperName = Scrappername;
                    p.New = false;
                }
            }
            finally
            {
                sema.Release();
            }
        });

        await Task.WhenAll(enrichmentTasks);
        return initialProductList;
    }


    public async Task<(bool ShouldSkip, string ExistingDescription)> ShouldSkip(string productUrl)
    {
        using var scope=_serviceProvider.CreateScope();
        var db= scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Sdata
            .Where(p => p.ProductUrl == productUrl && p.Enriched==true )
            .FirstOrDefaultAsync();

        bool shouldSkip = existing != null;

        string description = existing?.Description ?? "";

        return (shouldSkip, description);
    }
    private async Task Get_attributes(ShopifyFlatProduct p, string url)
    {
        try
        {
            var link = $"{url}products/{p.Handle}";
      
            var doc = await LoadPagewithRetry(link);

            // p.Sizes = Getsovanchesizes(doc);
            p.ProductUrl = link;
            p.Description = Getdescription(doc);
            p.Retail_Price = GetRetail_Price(doc)??0;
            p.ScraperName = Scrappername;
            p.New = true;
            p.Condition = GetCondition(doc);
            p.ConditionGrade = p.Condition=="New"?"New": GetConditionGrade(doc);
            p.Enriched=true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing product {p.Handle}: {ex.Message}");
        }
    }

    private string GetGender(List<string> tags)
    {
        if (tags.Contains("Male")) return "Men";
        if (tags.Contains("Female")) return "Women";
        if (tags.Contains("Unisex")) return "Unisex";
        return string.Empty;
    }

    private HttpClient CurrentEnrichClient()
    {
        lock (_enrichClientLock) return _enrichClient;
    }

    // Swap to the next proxy. The old client is not disposed because other enrichment tasks may still be mid-request on it.
    private HttpClient RotateEnrichClient(HttpClient failed, string reason)
    {
        lock (_enrichClientLock)
        {
            if (!ReferenceEquals(_enrichClient, failed)) return _enrichClient; // another task already rotated
            _enrichClient = GetProxyClient();
            _requestsOnCurrentClient = 0;
            _looger.LogInformation("Enrichment switched proxy ({Reason})", reason);
            return _enrichClient;
        }
    }

    public async Task<HtmlDocument> LoadPagewithRetry(string url, int maxtry = 4)
    {
        for (int attempt = 1; attempt <= maxtry; attempt++)
        {
            var client = CurrentEnrichClient();
            if (Interlocked.Increment(ref _requestsOnCurrentClient) > RequestsPerProxy)
            {
                client = RotateEnrichClient(client, $"{RequestsPerProxy} requests sent");
            }
            try
            {
                return await LoadPage(url, client);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden)
            {
                if (attempt == maxtry)
                    throw;

                // The limit/ban is per IP, so move to a fresh proxy instead of sleeping a minute on the same one
                _looger.LogWarning("{Status} for {Url}; retry {Attempt}/{Max} on next proxy",
                    (int)ex.StatusCode!, url, attempt, maxtry);
                RotateEnrichClient(client, $"HTTP {(int)ex.StatusCode}");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                if (attempt == maxtry)
                    throw;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 5);

                _looger.LogWarning(
                    ex,
                    "Request failed for {Url}. Waiting {Delay}s before retry {Attempt}/{Max}.",
                    url,
                    delay.TotalSeconds,
                    attempt,
                    maxtry);

                await Task.Delay(delay);
            }
        }

        throw new Exception("Unreachable");
    }

    private async Task<HtmlDocument> LoadPage(string url,HttpClient client)
    {
      
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

      
     
    

      
        await Task.Delay(Random.Shared.Next(2000, 4000));

        var res = await client.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // No sleep here: LoadPagewithRetry rotates to another proxy, which is faster than waiting out the limit
            throw new HttpRequestException(
                "429 Too Many Requests",
                null,
                HttpStatusCode.TooManyRequests);
        }

        res.EnsureSuccessStatusCode();

        var html = await res.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return doc;
    }

    private static string RandomUserAgent()
    {
        var rand = new Random();
        return agents[rand.Next(agents.Length)];
    }

    private static int RandomDelay() => new Random().Next(2000, 4000);

    private List<string> Getsovanchesizes(HtmlDocument doc)
    {
        List<string> availableSizes = new List<string>();

        var fieldset = doc.DocumentNode.SelectSingleNode("//fieldset[contains(@class,'product-form__input--pill')]");
        if (fieldset != null)
        {
            var sizeInputs = fieldset.Descendants("input")
                .Where(input => input.GetAttributeValue("type", "") == "radio" &&
                                !input.GetClasses().Contains("disabled"))
                .Select(input => input.GetAttributeValue("value", ""))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            if (sizeInputs.Any())
            {
                availableSizes = sizeInputs;
            }
        }

        if (!availableSizes.Any())
        {
            var span = doc.DocumentNode.SelectSingleNode("//div[div[contains(text(), 'Size')]]//span[contains(@class, 'metafield-multi_line_text_field')]");
            if (span != null)
            {
                var rawText = span.InnerText;
                availableSizes = rawText
                    .Split(',')
                    .Select(size => size.Trim())
                    .Where(size => !string.IsNullOrWhiteSpace(size))
                    .ToList();
            }
        }

        return availableSizes;
    }

    private string Getdescription(HtmlDocument doc)
    {
        var desdiv = doc.DocumentNode.SelectSingleNode("//div[div[contains(text(), 'Details')]]//span[contains(@class, 'metafield-multi_line_text_field')]");
        return desdiv != null ? ExtractTextWithLineBreaks(desdiv) : string.Empty;
    }
    private string GetCondition(HtmlDocument doc)
    {
        var conditionNode = doc.DocumentNode.SelectSingleNode(
                "//div[div[contains(text(), 'Condition')]]/div[contains(@class, 'tw-mb-8')]/p"
            );

        return conditionNode != null ? conditionNode.InnerText.Trim() : string.Empty;
    }

    private string GetConditionGrade(HtmlDocument doc)
    {
        var gradeNode = doc.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'tw-font-medium') and contains(text(), 'Pre-Owned')]"
        );

        if (gradeNode != null)
        {
            var text = gradeNode.InnerText.Trim();
            var parts = text.Split(new[] { "Pre-Owned." }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[0].Trim(); 
            }
        }

        return string.Empty;
    }
    private decimal? GetRetail_Price(HtmlDocument doc)
    {
        var priceNode = doc.DocumentNode
        .SelectSingleNode("//span[contains(@class, 'metafield-multi_line_text_field') and contains(text(), '$')]");

        if (priceNode == null) return null;

        var text = priceNode.InnerText.Trim();

        // Remove the '$' and any commas, then parse to decimal
        if (decimal.TryParse(text.Replace("$", "").Replace(",", ""), out decimal price))
        {
            return price;
        }

        return null;
    }

    private static string ExtractTextWithLineBreaks(HtmlNode node)
    {
        var sb = new StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            if (child.Name == "br")
                sb.AppendLine();
            else if (child.NodeType == HtmlNodeType.Text)
                sb.Append(child.InnerText.Trim());
        }
        return sb.ToString();
    }
    private HttpClient GetProxyClient()
    {
        // Same as ShoipfyScrapper: since 2026-10-12 the supplier 403s requests carrying the
        // Accept / Accept-Encoding combo, while a bare User-Agent request is accepted.
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
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

        return client;
    }
}
