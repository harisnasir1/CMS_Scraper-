

using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface Scrap_shopify{

    Task<ShopifyGetAllProductsResponse> Getproducts(string url);
}