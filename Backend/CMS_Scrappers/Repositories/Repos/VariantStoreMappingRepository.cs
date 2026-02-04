using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Repositories.Repos;
using CMS_Scrappers.Models;
public class VariantStoreMappingRepository:IVariantStoreMappingRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public VariantStoreMappingRepository(AppDbContext context, ILogger<ProductRepository> logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<bool> InsertVariantMapping(VariantStoreMapping variantm)
    {
        try
        {
           await  _context.VariantStoreMapping.AddAsync(variantm);
           await _context.SaveChangesAsync();
           return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error inserting Variant Mapping{ex}");
            return false;      
        }
    }

    public async Task<bool> Exist_VariantVariantMapping_BY_variantid(long id,Guid pmid)
    {
        try
        {
            var v= await _context.VariantStoreMapping.FirstOrDefaultAsync(v=>v.VariantId==id && v.ProductStoreMappingId==pmid);
            if (v == null)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error Deleting Variant Mapping by id{ex}");
            return false;      
        }
    }

    public async Task<bool> DelteVariantMapping(Guid id)
    {
        try
        {
            if (id == null)
            {
                _logger.LogError("passing bad variantmapping id");
                return false;
            }
            _context.Remove(_context.VariantStoreMapping.Single(vm => vm.Id == id));
             await _context.SaveChangesAsync();
             return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error Deleting Variant Mapping by id{ex}");
            return false;      
        }
    }

    public async Task<bool> DeleteAllVariantMapping_per_productmapping(Guid id)
    {
        try
        {
            if (id == null)
            {
                _logger.LogError("passing bad variantmapping id");
                return false;
            }
            _context.Remove(_context.VariantStoreMapping.Single(vm => vm.ProductStoreMappingId == id));
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error Deleting Variant Mapping by product mapping id{ex}");
            return false;      
        }
    }
    
}