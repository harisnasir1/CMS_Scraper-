using CMS_Scrappers.Models;

namespace CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Data.DTO;
public interface IProductStoreMappingRepository
{
    Task<bool> InsertProductmapping(ProductStoreMapping data);
    
    Task<string> GetSyncIdBySidAndStoreId(Guid sid,Guid storeId);

    Task<bool> Update_Status(Guid id, string status);
}