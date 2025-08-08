using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

public interface IShopifyScrapperFact{
    ShopifyStoreScraper CreateScraper(string StoreName);
}