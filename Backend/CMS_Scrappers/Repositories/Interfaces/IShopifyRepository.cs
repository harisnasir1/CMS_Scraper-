using System.ComponentModel.DataAnnotations;

namespace CMS_Scrappers.Repositories.Interfaces;

public interface IShopifyRepository
{
    Task<List<Shopify>> GiveallStoresToSync();

    Task<Shopify?> GiveStoreById(Guid storeId);  

    Task<bool> LastSynced(Guid storeId, int variantadded );
}