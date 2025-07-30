using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace CMS_Scrappers.Repositories.Repos
{
    public class ProductRepository:IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductRepository> _logger;
        public ProductRepository(AppDbContext context,ILogger<ProductRepository> logger) {
         _logger=logger;
          _context = context;
        }  

        public async Task <List<Sdata>> GiveProducts(Guid scraper, int PageNumber, int PageSize)
        {
            _logger.LogError($"page number ={PageNumber} \n pagesize ={PageSize}");
            return await _context.Sdata
                  .Where(s => s.Sid == scraper && s.Status== "Categorized")
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }
        public async Task<List<Sdata>> GetPendingReviewproducts(int PageNumber, int PageSize)
        {
            return await _context.Sdata
                  .Where(s =>  s.Status == "Categorized")
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }

    }
}
