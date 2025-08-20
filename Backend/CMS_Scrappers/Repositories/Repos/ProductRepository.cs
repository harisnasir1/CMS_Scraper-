using System;
using Amazon.S3.Model;
using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
                  .Where(s =>  s.Status == "Categorized" && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton")&& (s.ProductType != "" || s.Category!= ""))
                  .Include(s => s.Image)
                  .Include(s => s.Variants)
                  .Where(s => s.Variants.Any(v => v.InStock))
                  .OrderByDescending(s => s.CreatedAt)
                  .Skip((PageNumber - 1) * PageSize)
                   .Take(PageSize)
                  .ToListAsync();
        }
        public async Task<List<Sdata>> GetLiveproducts(int PageNumber, int PageSize)
        {
            return await _context.Sdata
                  .Where(s => s.Status == "Live" && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton") && (s.ProductType != "" || s.Category != ""))
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

        public async Task UpdateImages(Guid id,List<ProductImageRecordDTO> updatedImages)
        {
            try
            {
                var data = await _context.Sdata
                 .Include(s => s.Image)
                 .FirstOrDefaultAsync(s => s.Id == id);

                if (data == null)
                    throw new Exception("Sdata not found");

                var finalimages = new List<ProductImageRecord>();

                foreach (var oldimg in data.Image)
                {
                    var newimg = updatedImages.FirstOrDefault(x => x.Id == oldimg.Id.ToString());
                    if (newimg == null)
                    {
                        finalimages.Add(oldimg);
                    }
                    else
                    {
                        finalimages.Add(
                            new ProductImageRecord
                            {
                                Id = oldimg.Id,
                                SdataId = oldimg.SdataId,
                                Url = newimg.Url,
                                Priority = newimg.Priority,
                            }
                            );
                    }
                }

                var newimgs=updatedImages.Where(imgs=>string.IsNullOrEmpty(imgs.Id)).ToList();
               if(newimgs!=null){
                    foreach (var nimg in newimgs)
                    {
                        finalimages.Add(new ProductImageRecord
                        {
                            SdataId = data.Id,
                            Url = nimg.Url,
                            Priority = nimg.Priority,
                        });
                    }
                }
                finalimages=finalimages.OrderBy(o=>o.Priority).ToList();
                data.Image= finalimages;
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

         try{
                var strategy = _context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    var product = await _context.Sdata.FirstOrDefaultAsync(p => p.Id == id);
                    if (product == null) return false;

                    var current = product.Status;
                    bool allowed = (current, status) switch
                    {
                        ("Categorized", "Shopify Queued") => true,
                        ("Shopify Queued", "Processing") => true,
                        ("Processing", "Live") => true,
                        ("Processing", "Failed") => true,
                        _ => false
                    };

                    if (!allowed) return false;

                    product.Status = status;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                });
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
            return await _context.Sdata
                .Where(s=>s.Status==status && s.Condition == "New" && (s.Brand == "Chrome Hearts" || s.Brand == "Louis Vuitton") && (s.ProductType != "" || s.Category != ""))
                 .Include(s => s.Variants)
                  .Where(s => s.Variants.Any(v => v.InStock))
                .CountAsync();
        }
         


    }
}
