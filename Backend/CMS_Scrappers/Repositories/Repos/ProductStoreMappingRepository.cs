using CMS_Scrappers.Repositories.Interfaces;
using  CMS_Scrappers.Data.DTO;
using CMS_Scrappers.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Repositories.Repos;

public class ProductStoreMappingRepository:IProductStoreMappingRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger;
    public ProductStoreMappingRepository(AppDbContext context,ILogger<ProductRepository> logger) {
        _logger=logger;
        _context = context;
    }  
   public async Task<Guid> InsertProductmapping(ProductStoreMapping data)
   {
       try
       {
           await _context.ProductStoreMapping.AddAsync(data);

           await _context.SaveChangesAsync();
           
           return data.Id;
       }
       catch (Exception ex)
       {
           _logger.LogCritical($"Error inserting Product Mapping{ex}");
           return Guid.Empty;      
       }
   
   }

   public async Task<string> GetSyncIdBySidAndStoreId(Guid sid, Guid storeId)
   {
       try
       {
           return await _context.ProductStoreMapping.Where(s => s.ProductId == sid && s.ShopifyStoreId == storeId)
               .Select(s=>s.ExternalProductId)
               .SingleOrDefaultAsync();
          
       }
       catch (Exception e)
       {
           Console.WriteLine(e);
           throw;
           
       }
   }
}