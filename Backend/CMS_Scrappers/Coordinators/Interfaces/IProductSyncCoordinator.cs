namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IProductSyncCoordinator
{
    Task<bool> pushProductslive(Sdata data);

    Task UpdateProduct_Coordinator(List<ShopifyFlatProduct> existingproduct);

    /// <summary>Removes ONE supplier's variants from the stores when the scrape has not seen them for 24h.</summary>
    Task DeleteLiveProducts(Guid scraperId);

    Task CleanupSourceDeletedFromShopify();

    Task<(int Deleted, int Failed)> DeleteProductFromAllStores(Guid sdataId);

    Task<bool> BulkSyncLiveProduct(Guid store);
}