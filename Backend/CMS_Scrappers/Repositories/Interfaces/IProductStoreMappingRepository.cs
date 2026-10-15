using CMS_Scrappers.Models;

namespace CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Data.DTO;
public interface IProductStoreMappingRepository
{
    Task<Guid> InsertProductmapping(ProductStoreMapping data);
    
    Task<string> GetSyncIdBySidAndStoreId(Guid sid,Guid storeId);
    Task<ProductStoreMapping> GetProductStoreMapping(Guid id);
    Task DeleteProductStoreMapping(Guid id);
    /// <summary>Flags product mappings whose Shopify product no longer exists; they are skipped by every push path from then on.</summary>
    Task<int> MarkRemoved(List<Guid> ids);
    Task<List<ProductStoreMapping>> GetAllOrphanedProductStores(Guid storeId);
}