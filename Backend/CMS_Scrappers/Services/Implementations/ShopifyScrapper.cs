
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
        var pageNumber = 1;

        while (true)
        {
            var httpResponse =
                await _httpClient.GetAsync($"{url}/products.json?limit=250&page={pageNumber}");

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception();

            var productsResponse = await httpResponse.Content.ReadFromJsonAsync<ShopifyStoreProductsResponse>();

            if (productsResponse?.Products.Count == 0 || productsResponse==null )
            {
                break;
            }
            if(pageNumber>10)
            {
                break;
            }
            
            response.Pages.Add(productsResponse);
            pageNumber++;
            await Task.Delay(2000);
        }
        return response;
    }


}