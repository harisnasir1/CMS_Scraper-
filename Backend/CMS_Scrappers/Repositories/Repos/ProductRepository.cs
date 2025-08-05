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
                  .OrderByDescending(s => s.CreatedAt)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }
        public async Task<Sdata> Getproductbyid(Guid productid)
        {
           return await _context.Sdata
        .Include(s => s.Image)       
        .FirstOrDefaultAsync(s => s.Id == productid);

        }

        public async Task UpdateImages(Guid id,List<ProductImageRecord> updatedImages)
        {
            var data = await _context.Sdata
                .Include(s => s.Image)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (data == null)
                throw new Exception("Sdata not found");

            foreach (var updatedImage in updatedImages)
            {
                var existingImage = data.Image.FirstOrDefault(img => img.Id == updatedImage.Id);
                if (existingImage != null)
                {
                    existingImage.Url = updatedImage.Url;   
                }
                else
                {
                    data.Image.Add(updatedImage);
                }
            }
        
            var updatedImageIds = updatedImages.Select(i => i.Id).ToHashSet();
            var imagesToRemove = data.Image.Where(img => !updatedImageIds.Contains(img.Id)).ToList();

            foreach (var imgToRemove in imagesToRemove)
            {
                data.Image.Remove(imgToRemove);
               
            }

            data.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

    }
}
