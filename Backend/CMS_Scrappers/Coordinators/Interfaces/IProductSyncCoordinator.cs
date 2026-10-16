namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IProductSyncCoordinator
{
    Task<bool> pushProductslive(Sdata data);

    Task UpdateProduct_Coordinator(List<ShopifyFlatProduct> existingproduct);

    Task DeleteLiveProducts();

    Task CleanupSourceDeletedFromShopify();

    Task<(int Deleted, int Failed)> DeleteProductFromAllStores(Guid sdataId);

    Task<bool> BulkSyncLiveProduct(Guid store);
}