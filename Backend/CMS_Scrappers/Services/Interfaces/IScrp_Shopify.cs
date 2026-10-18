using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface Scrap_shopify{

    /// <param name="storefrontPassword">
    /// Shopify storefront password for password-protected stores. Null or empty for public stores.
    /// </param>
    /// <param name="maxPages">Stop after this many pages; null fetches the whole catalogue.</param>
    Task<ShopifyGetAllProductsResponse> Getproducts(string url, string? storefrontPassword = null, int? maxPages = null);
}