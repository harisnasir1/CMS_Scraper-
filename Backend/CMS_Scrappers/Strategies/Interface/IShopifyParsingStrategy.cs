using CMS_Scrappers.Suppliers;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface IShopifyParsingStrategy
{
    /// <summary>Raw products.json pages -> flat products for the given supplier. The profile carries name and base URL.</summary>
    Task<List<ShopifyFlatProduct>> MapAndEnrichProductAsync(ShopifyGetAllProductsResponse rawProduct, SupplierProfile profile);
    Task<(bool ShouldSkip, string ExistingDescription)> ShouldSkip(string productUrl);
}