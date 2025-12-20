using CMS_Scrappers.Repositories.Interfaces;
using  CMS_Scrappers.Data.DTO;
using CMS_Scrappers.Models;

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
}