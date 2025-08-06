using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
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

    public SavonchesStrategy(
        Scrap_shopify scrap_Shopify,
        IHttpClientFactory clientFactory,
        IScrapperRepository scrapperRepository,
        ILogger<SavonchesStrategy> logger,
        AppDbContext context,
        IServiceProvider serviceProvider)
    {
        _looger = logger;
        _Scrap_shopify = scrap_Shopify;
        _scrapperRepository = scrapperRepository;
        _client = clientFactory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
        _context = context;
        _serviceProvider = serviceProvider;
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

        var sema = new SemaphoreSlim(5); 

        var enrichmentTasks = initialProductList.Select(async p =>
        {
            await sema.WaitAsync();
            try
            {
               
                var fullUrl = $"{storeBaseUrl}products/{p.Handle}";
                var skipResult = await ShouldSkip(fullUrl);
                if (!skipResult.ShouldSkip)
                {
                    await Task.Delay(RandomDelay());
                    await Get_attributes(p, storeBaseUrl); 
                }
                else
                {
                    _looger.LogWarning($"rolling back from the {fullUrl}");
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
        if (tags.Contains("Male")) return "Male";
        if (tags.Contains("Female")) return "Female";
        if (tags.Contains("Unisex")) return "Unisex";
        return string.Empty;
    }

    public async Task<HtmlDocument> LoadPagewithRetry(string url, int maxtry = 4)
    {
        int delay = 9000;

        for (int attempt = 1; attempt <= maxtry; attempt++)
        {
            try
            {
                return await LoadPage(url);
            }
            catch (Exception ex)
            {
                if (attempt < maxtry)
                {
                    _looger.LogError(ex, $"Retrying loading page for {url} (attempt {attempt})");
                    await Task.Delay(delay);
                    delay *= 2;
                }
                else
                {
                    _looger.LogError(ex, $"Failed to load page after {attempt} attempts: {url}");
                    throw;
                }
            }
        }

        throw new Exception("Unreachable code: LoadPagewithRetry failed all retries.");
    }

    private async Task<HtmlDocument> LoadPage(string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd(RandomUserAgent());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        req.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var res = await _client.SendAsync(req);
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
}
