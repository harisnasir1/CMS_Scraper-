namespace CMS_Scrappers.Repositories.Interfaces;

public interface IShopifyRepository
{
    Task<List<Shopify>> GiveallStoresToSync();
}