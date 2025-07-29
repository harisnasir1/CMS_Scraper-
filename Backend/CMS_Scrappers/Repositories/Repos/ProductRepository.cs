using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace CMS_Scrappers.Repositories.Repos
{
    public class ProductRepository:IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context) {
          _context = context;
        }  

        public async Task <List<Sdata>> GiveProducts(Guid scraper, int PageNumber, int PageSize)
        {
            return await _context.Sdata
                  .Where(s => s.Sid == scraper && s.Status== "Categorized")
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .Skip((PageNumber - 1) * PageSize)
                  .ToListAsync();
        }
    }
}
