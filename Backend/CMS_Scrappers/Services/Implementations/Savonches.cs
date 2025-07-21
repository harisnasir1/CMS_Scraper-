using HtmlAgilityPack;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

public class Savonches : IScrappers
{
    private static readonly string Scrappername="Savonches";
    private   DateTime TimeStart {get;set;}
    private   DateTime TimeEnd {get;set;} 
    private static readonly string baseUrl = "https://savonches.com/";
    private static readonly string producturl = $"{baseUrl}/products";
    private readonly HttpClient _client;
    private readonly Scrap_shopify _Scrap_shopify;

    private readonly IScrapperRepository _scrapperRepository;

    public Savonches(Scrap_shopify scrap_Shopify, IHttpClientFactory clientFactory,IScrapperRepository scrapperRepository)
    {
        _Scrap_shopify = scrap_Shopify;
        _scrapperRepository=scrapperRepository;
        _client = clientFactory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    private static readonly string[] agents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Linux; Android 10; Pixel 3 XL) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0 Mobile Safari/537.36",
    };

    private static string RandomUserAgent()
    {
        var rand = new Random();
        return agents[rand.Next(agents.Length)];
    }

    public async Task ScrapeAsync()
    {
        try
        {
            Console.WriteLine("Starting scraping process...");
           
           var start =await _scrapperRepository.Startrun("Savonches");
           if(!start) return;
            
            TimeStart=DateTime.UtcNow;
            
            var allproducts = await _Scrap_shopify.Getproducts(producturl);
           
            foreach (var p in allproducts)
            {
                try
                {
                    var link = $"{baseUrl}products/{p.Handle}";
                    await Task.Delay(RandomDelay());
                    var doc = await LoadPage(link);

                    var imgNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'product__media')]//img");
                    var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'product__title')]");

                    string? name = titleNode?.SelectSingleNode(".//small")?.InnerText?.Trim();
                    string? brand = titleNode?.SelectSingleNode(".//h1")?.InnerText?.Trim();
                    string? fullName = titleNode?.SelectSingleNode(".//h2")?.InnerText?.Trim();
                    string? price = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'price-item--regular')]")?.InnerText?.Trim();

                    string? imgSrc = imgNode?.GetAttributeValue("src", null);
                    if (imgSrc != null && imgSrc.StartsWith("//")) imgSrc = "https:" + imgSrc;

                    Console.WriteLine("----------------product-------------- ");
                    Console.WriteLine($"url: {link}");
                    Console.WriteLine($"Name: {name}");
                    Console.WriteLine($"Brand: {brand}");
                    Console.WriteLine($"Price: {price}");
                    Console.WriteLine($"Image: {imgSrc}");
                    Console.WriteLine("-----------------end-------------------\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing product {p.Handle}: {ex.Message}");
                    continue; // Continue with next product
                }
            }

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
}