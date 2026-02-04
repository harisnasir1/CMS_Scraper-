using Amazon.Runtime.Internal.Util;
using CMS_Scrappers.Models;
namespace CMS_Scrappers.Repositories.Interfaces;

public interface IVariantStoreMappingRepository
{
    Task<bool> InsertVariantMapping( VariantStoreMapping variants);

    Task<bool> Exist_VariantVariantMapping_BY_variantid(long id);
    
    Task<bool> DelteVariantMapping(Guid id);

    Task<bool> DeleteAllVariantMapping_per_productmapping(Guid id);
}