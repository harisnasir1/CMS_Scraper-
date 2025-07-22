using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface IShopifyParsingStrategy
{
    Task<List<ShopifyFlatProduct>> MapAndEnrichProductAsync(ShopifyGetAllProductsResponse rawProduct, string storeBaseUrl);
}