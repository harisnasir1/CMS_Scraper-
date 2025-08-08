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
                  .Where(s => s.Sid == scraper && s.Status== "Categorized"&&s.Condition=="New" && (s.Brand== "Chrome Hearts" || s.Brand == "Louis Vuitton"))
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }
        public async Task<List<Sdata>> GetPendingReviewproducts(int PageNumber, int PageSize)
        {
            return await _context.Sdata
                  .Where(s =>  s.Status == "Categorized" && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton"))
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .OrderByDescending(s => s.CreatedAt)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }
        public async Task<List<Sdata>> GetLiveproducts(int PageNumber, int PageSize)
        {
            return await _context.Sdata
                  .Where(s => s.Status == "Live" && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton"))
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
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == productid);
        }

        public async Task UpdateImages(Guid id,List<ProductImageRecord> updatedImages)
        {
           try{ var data = await _context.Sdata
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");
               
            }
        }

        public async Task UpdateDescription(Guid id,string desc)
        {
           try{ var data=await this.Getproductbyid(id);
            if (data == null) throw new Exception("id is not valid to update Description");
            data.Description= desc;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");

            }
        }

        public async Task<bool> AddShopifyproductid(Sdata sdata,string Shopifyid)
        {
           try{ var data = await this.Getproductbyid(sdata.Id);
            if (data == null){
                return false;
            }
            data.Shopifyid=Shopifyid;
            await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");
                return false;
            }

        }
        public async Task <bool> UpdateStatus(Guid id,string status)
        {

         try{   var data= await _context.Sdata.FindAsync(id);
            if (data == null) return false;
            data.Status=status;
            await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");
                return false;
            }
        }

        public async Task<bool> UpdateProductDetailsAsync(Guid id,string sku, string title, string description, int price)
        {
            try
            {
                var product = await Getproductbyid(id);

                if (product == null)
                {
                    _logger.LogWarning($"Product with SKU '{sku}' not found.");
                    return false;
                }

                product.Title = title;
                product.Description = description;
                product.Price = price;
                product.UpdatedAt = DateTime.UtcNow;
                product.Sku = sku;

                _context.Sdata.Update(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Product with SKU '{id}' updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product details.");
                return false;
            }
        }
        public async Task<int> TotalStatusProdcuts(string status)
        {
            return await _context.Sdata.Where(s=>s.Status==status && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton")).CountAsync();
        }

    }
}
