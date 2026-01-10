namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IProductSyncCoordinator
{
    Task<bool> pushProductslive(Sdata data);
    
    Task UpdateProduct_Coordinator(List<ShopifyFlatProduct> existingproduct);

    Task<bool> BulkSyncLiveProduct(Guid store);

    Task<bool> OrphanedProductCleanupAsync();
}