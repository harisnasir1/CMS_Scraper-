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
   public async Task<bool> InsertProductmapping(ProductStoreMapping data)
   {
       try
       {
           await _context.ProductStoreMapping.AddAsync(data);

           await _context.SaveChangesAsync();
           
           return true;
       }
       catch (Exception ex)
       {
           return false;      
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

   public async Task<bool> Update_Status(Guid id, string status)
   {
       try
       {
           var mapping= await _context.ProductStoreMapping.FirstOrDefaultAsync(s => s.ProductId == id);
           if (mapping == null) return false;
           var current = mapping.SyncStatus;
           bool allowed = (current, status) switch
           {
               ("Live", "Deleted") => true,
               _ => false
           };
           if (!allowed) return false;
           mapping.SyncStatus = status;
           await _context.SaveChangesAsync();
           return true;
       }
       catch (Exception e){
           Console.WriteLine(e);
           throw;
       }
   }
   
   
}