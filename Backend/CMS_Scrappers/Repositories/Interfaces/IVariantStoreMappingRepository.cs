using Amazon.Runtime.Internal.Util;
using CMS_Scrappers.Models;
namespace CMS_Scrappers.Repositories.Interfaces;

public interface IVariantStoreMappingRepository
{
    Task<bool> InsertVariantMapping( VariantStoreMapping variants);

    Task<List<VariantStoreMapping>> Get_ProfcutStoreMapping_AllVariants(Guid id);

    Task<bool> UpdateStockAndPrice(List<(long variantId, Guid productStoreMappingId, bool instock, decimal price)> updates);

    /// <summary>Every variant mapping for a store, with its supplier variant loaded.</summary>
    Task<List<VariantStoreMapping>> GetAllForStore(Guid storeId);

    /// <summary>Sets InStock on the given mappings; only rows whose flag differs are touched. Returns how many changed.</summary>
    Task<int> UpdateInStockFlags(List<(Guid mappingId, bool inStock)> states);

    Task<bool> Exist_VariantVariantMapping_BY_variantid(long id,Guid pmid);
    
    Task<bool> DelteVariantMapping(Guid id);
    Task DeleteAllVariants(List<Guid> ids);
    Task<bool> DeleteAllVariantMapping_per_productmapping(Guid id);
}