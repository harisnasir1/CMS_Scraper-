using HtmlAgilityPack;
using System.Net.Http.Headers;

public class Savonches:IScrappers
{
    private static readonly string baseUrl = "https://savonches.com/";
    private static readonly HttpClient client = new();

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

    public async void ScrapeAsync()   //need to chang this when we store int he db
    {
        var productLinks = new List<string>();
        
        for (int page = 1; page <= 2; page++)
        {
            await Task.Delay(RandomDelay());
            var url = $"https://savonches.com/collections/new-arrivals?page={page}";

            var doc = await LoadPage(url);
            var products = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-card-wrapper')]");

            if (products == null) continue;

            foreach (var item in products)
            {
                var links = item.Descendants("a")
                                .Where(a => a.Attributes.Contains("href"))
                                .Select(a => a.Attributes["href"].Value)
                                .Distinct();

                foreach (var href in links)
                {
                    var full = href.StartsWith("/") ? baseUrl + href.TrimStart('/') : href;
                    if (!productLinks.Contains(full)) productLinks.Add(full);
                }
            }
        }

        foreach (var link in productLinks)
        {
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
    }

    private async Task<HtmlDocument> LoadPage(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd(RandomUserAgent());
        var res = await client.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var html = await res.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    private static int RandomDelay() => new Random().Next(1500, 3500);
}