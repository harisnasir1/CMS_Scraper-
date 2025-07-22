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
            var endpoint = $"https://savonches.com/products.json?limit=2&page={page}";

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Request failed with status code {response.StatusCode}");

            var parsed = await response.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();
            
         
            if (parsed?.Products == null || parsed.Products.Count == 0)
                break;

            foreach (var product in parsed.Products)
            {
               decimal priceDecimal = 0;
               decimal.TryParse(product?.Variants[0].Price,out priceDecimal);
               string Ge=GetGender(product.Tags);
              
                var flat = new ShopifyFlatProduct
                {
                    Id = product.Id,
                    Title = product.Title,
                    Handle = product.Handle,
                    ImageUrls = product.Images?.Select(img => img.Src).ToList() ?? new List<string>(),
                    Category=product.ProductType,
                    Price=priceDecimal,
                    Brand=product.Vendor,
                    Gender=Ge
                };
                allProducts.Add(flat);
            }
            break;
            await Task.Delay(5000); 
           
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while fetching products: {ex.Message}");
      
        throw;
    }
   
    return allProducts;
}

 private string GetGender(List<string> tags)
 {
              string Gender="";
               if(tags.Contains("Male"))
               {
                  return "Male";
               }
               else if (tags.Contains("Female"))
               {
                 return"Female";
               }
               else if(tags.Contains("Unisex")){
                 return"Unisex";
               }
               else{
                return" ";
               }
 }
}