using System.Net.Http.Json;
namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using System.Text.Json;

public class ShoipfyScrapper:Scrap_shopify{

    private readonly HttpClient _httpClient;

    public ShoipfyScrapper(HttpClient httpClient)
    {
        _httpClient=new HttpClient();
         _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd( "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0");
    }

  public async Task<List<ShopifyFlatProduct>> Getproducts(string url)
{
    var allProducts = new List<ShopifyFlatProduct>();
    var page = 1;

    try
    {
        while (true)
        {
            var endpoint = $"https://savonches.com/products.json?limit=550&page={page}";

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Request failed with status code {response.StatusCode}");

            var parsed = await response.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();
            
         
            if (parsed?.Products == null || parsed.Products.Count == 0)
                break;

            foreach (var product in parsed.Products)
            {
                var flat = new ShopifyFlatProduct
                {
                    Id = product.Id,
                    Title = product.Title,
                    Handle = product.Handle,
                    ImageUrls = product.Images?.Select(img => img.Src).ToList() ?? new List<string>()
                };

                allProducts.Add(flat);

            }
            await Task.Delay(5000); 

           break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while fetching products: {ex.Message}");
        // Optionally, rethrow or handle error accordingly
        throw;
    }
   
    return allProducts;
}

}