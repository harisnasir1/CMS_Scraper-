
namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public class ShoipfyScrapper:Scrap_shopify{

    private readonly HttpClient _httpClient;

    public ShoipfyScrapper()
    {
        _httpClient=new HttpClient();
         _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd( "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0");
    }

  public async Task<ShopifyGetAllProductsResponse> Getproducts(string url)
{
          var response = new ShopifyGetAllProductsResponse();
        var pageNumber = 340;

        while (true)
        {
            var httpResponse =
                await _httpClient.GetAsync($"{url}/products.json?limit=2&page={pageNumber}");

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception();

            var productsResponse = await httpResponse.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();

            if (productsResponse?.Products.Count == 0)
            {
                break;
            }
            
            response.Pages.Add(productsResponse);
            pageNumber++;
            break;

        }

        return response;
    }


}