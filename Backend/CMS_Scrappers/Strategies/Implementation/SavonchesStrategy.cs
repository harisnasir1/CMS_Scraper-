using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
public class SavonchesStrategy : IShopifyParsingStrategy
{

    private static readonly string Scrappername = "SavonchesStrategy";
    private readonly ILogger<SavonchesStrategy> _looger;
  
    private readonly HttpClient _client;
    private readonly Scrap_shopify _Scrap_shopify;

    private readonly IScrapperRepository _scrapperRepository;
    public SavonchesStrategy(Scrap_shopify scrap_Shopify, IHttpClientFactory clientFactory, IScrapperRepository scrapperRepository, ILogger<SavonchesStrategy> logger)
    {
        _looger = logger;
        _Scrap_shopify = scrap_Shopify;
        _scrapperRepository = scrapperRepository;
        _client = clientFactory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
    }
    private static readonly string[] agents = new[]
{
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Linux; Android 10; Pixel 3 XL) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0 Mobile Safari/537.36",
    };
    public async Task<List<ShopifyFlatProduct>> MapAndEnrichProductAsync(ShopifyGetAllProductsResponse rawProduct, string storeBaseUrl)
    {
      
        var allProducts = new List<ShopifyFlatProduct>();
        foreach (var page in rawProduct.Pages)
        {
            foreach (var product in page.Products)
            {
                decimal priceDecimal = 0;
                decimal.TryParse(product?.Variants[0].Price, out priceDecimal);
                string Ge = GetGender(product.Tags);

                var flat = new ShopifyFlatProduct
                {
                    Id = product.Id,
                    Title = product.Title,
                    Handle = product.Handle,
                    ImageUrls = product.Images?.Select(img => img.Src).ToList() ?? new List<string>(),
                    Category = product.ProductType,
                    Price = priceDecimal,
                    Brand = product.Vendor,
                    Gender = Ge
                };
                await Get_attributes(flat,storeBaseUrl);
                allProducts.Add(flat);
            }
        }
        return allProducts;
    }
    private async Task Get_attributes(ShopifyFlatProduct p,string url)
    {
        try
        {
            var link = $"{url}products/{p.Handle}";
            await Task.Delay(RandomDelay());
            var doc = await LoadPage(link);

            p.Sizes = Getsovanchesizes(doc);
            p.Description = Getdescription(doc);
            p.ScraperName=Scrappername;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing product {p.Handle}: {ex.Message}");

        }



    }

    private string GetGender(List<string> tags)
    {
        string Gender = "";
        if (tags.Contains("Male"))
        {
            return "Male";
        }
        else if (tags.Contains("Female"))
        {
            return "Female";
        }
        else if (tags.Contains("Unisex"))
        {
            return "Unisex";
        }
        else
        {
            return " ";
        }
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

    private static int RandomDelay() => new Random().Next(1500, 3500);

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
        string Description = "";
        var desdiv = doc.DocumentNode.SelectSingleNode("//div[div[contains(text(), 'Details')]]//span[contains(@class, 'metafield-multi_line_text_field')]");
        if (desdiv != null)
        {
            Description = ExtractTextWithLineBreaks(desdiv);
        }
        return Description;
    }
    static string ExtractTextWithLineBreaks(HtmlNode node)
    {
        var sb = new StringBuilder();

        foreach (var child in node.ChildNodes)
        {
            if (child.Name == "br")
            {
                sb.AppendLine();
            }
            else if (child.NodeType == HtmlNodeType.Text)
            {
                sb.Append(child.InnerText.Trim());
            }
        }

        return sb.ToString();
    }

}