namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public class ShopifyGetAllProductsResponse
{
    public List<ShopifyStoreProductsResponse> Pages { get; } = new();
}