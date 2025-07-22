using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;

public class Savonches : IScrappers
{
    private static readonly string Scrappername="Savonches";
    private readonly ILogger<Savonches> _looger;
    private   DateTime TimeStart {get;set;}
    private   DateTime TimeEnd {get;set;}
    private static readonly string baseUrl = "https://savonches.com/";
    private static readonly string producturl = $"{baseUrl}/products";
    private readonly HttpClient _client;
    private readonly Scrap_shopify _Scrap_shopify;

    private readonly IScrapperRepository _scrapperRepository;

    private static readonly string[] agents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Linux; Android 10; Pixel 3 XL) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0 Mobile Safari/537.36",
    };





    public Savonches(Scrap_shopify scrap_Shopify, IHttpClientFactory clientFactory,IScrapperRepository scrapperRepository, ILogger<Savonches> logger)
    {
        _looger=logger;
        _Scrap_shopify = scrap_Shopify;
        _scrapperRepository=scrapperRepository;
        _client = clientFactory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    private static string RandomUserAgent()
    {
        var rand = new Random();
        return agents[rand.Next(agents.Length)];
    }

    public async Task ScrapeAsync()
    {
        try
        {
            _looger.LogInformation("Starting scraping process...");
           
           var start =await _scrapperRepository.Startrun("Savonches");
           if(!start) return;
            
            TimeStart=DateTime.UtcNow;
            
            var allproducts = await _Scrap_shopify.Getproducts(producturl);
           await Get_attributes(allproducts);
        

            TimeEnd=DateTime.UtcNow;
            TimeSpan Diff=TimeEnd-TimeStart;
            
          await  _scrapperRepository.Stoprun(Diff.ToString(),"Savonches");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error during scraping: {ex.Message}");
            throw;
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

    private static int RandomDelay() => new Random().Next(1500, 3500);

    private async Task Get_attributes(List<ShopifyFlatProduct> allpros)
    {
        List<ShopifyFlatProduct> allproducts=new List<ShopifyFlatProduct>();
        foreach (var p in allpros)
            {
                try
                {
                    var link = $"{baseUrl}products/{p.Handle}";
                    await Task.Delay(RandomDelay());
                    var doc = await LoadPage(link);

                    // var imgNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'product__media')]//img");
                    // var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'product__title')]");
                    // string? name = titleNode?.SelectSingleNode(".//small")?.InnerText?.Trim();
                    // string? brand = titleNode?.SelectSingleNode(".//h1")?.InnerText?.Trim();
                    // string? fullName = titleNode?.SelectSingleNode(".//h2")?.InnerText?.Trim();
                    // string? price = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'price-item--regular')]")?.InnerText?.Trim();
                    // string Condition=doc.DocumentNode.SelectSingleNode("//div[contains(@class,'tw-mb-8')]//p").InnerText?.Trim();
                    // string? imgSrc = imgNode?.GetAttributeValue("src", null);
                    // if (imgSrc != null && imgSrc.StartsWith("//")) imgSrc = "https:" + imgSrc;

                    List<string> availableSizes=Getsovanchesizes(doc);
                    string Des=Getdescription(doc);
                    
                    var f=new ShopifyFlatProduct{
                    Id = p.Id,
                    Title = p.Title,
                    Handle = p.Handle,
                    ImageUrls = p.ImageUrls,
                    Brand=p.Brand,
                    ScraperName=Scrappername,
                    Price=p.Price,
                    Category=p.Category,
                    Sizes=availableSizes,
                    Description=Des,
                    Gender=p.Gender
                    };
                    _looger.LogWarning(System.Text.Json.JsonSerializer.Serialize(f));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing product {p.Handle}: {ex.Message}");
                    continue; 
                }
            }
    }

     private  List<string> Getsovanchesizes( HtmlDocument doc)
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

     private  string Getdescription(HtmlDocument doc)
     {
        string Description="";
        var desdiv = doc.DocumentNode.SelectSingleNode("//div[div[contains(text(), 'Details')]]//span[contains(@class, 'metafield-multi_line_text_field')]");        
        if(desdiv !=null)
        {
             Description=ExtractTextWithLineBreaks(desdiv);
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