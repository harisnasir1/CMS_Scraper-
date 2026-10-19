using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface IShopifyScrapperFact{
 Task<ShopifyStoreScraper> CreateScraperAsync(string scraperName);
}