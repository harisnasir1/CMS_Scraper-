using Amazon.Runtime.Internal.Util;
using CMS_Scrappers.Models;
namespace CMS_Scrappers.Repositories.Interfaces;

public interface IVariantStoreMappingRepository
{
    Task<bool> InsertVariantMapping( VariantStoreMapping variants);

    Task<List<VariantStoreMapping>> Get_ProfcutStoreMapping_AllVariants(Guid id);

    Task<bool> UpdateStockAndPrice(List<(long variantId, Guid productStoreMappingId, bool instock, decimal price)> updates);

    Task<bool> Exist_VariantVariantMapping_BY_variantid(long id,Guid pmid);
    
    Task<bool> DelteVariantMapping(Guid id);

    Task<bool> DeleteAllVariantMapping_per_productmapping(Guid id);
}