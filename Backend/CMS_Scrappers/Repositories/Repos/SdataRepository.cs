using Microsoft.EntityFrameworkCore;

using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Amazon.S3.Model;

namespace CMS_Scrappers.Repositories.Repos
{
    public class SdataRepository : ISdataRepository
    {
        // This should ideally be retrieved from configuration or a user service,
        // but making it static readonly is better than 'new Guid()' in the constructor.
        private static readonly Guid _userId = new Guid("0b651c37-c448-42cd-a06e-e01144285502");
        private readonly AppDbContext _context;

        public SdataRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Add(List<ShopifyFlatProduct> data, Guid scraperId)
        {
            var newProducts = data.Where(p => p.New).ToList();
            var existingProducts = data.Where(p => !p.New).ToList();

          
            if (existingProducts.Any())
            {
                var productUrls = existingProducts.Select(p => p.ProductUrl).ToList();
                var dbProductsDict = await _context.Sdata
                    .Include(s => s.Variants)
                    .Where(s => productUrls.Contains(s.ProductUrl))
                    .ToDictionaryAsync(s => s.ProductUrl);

                foreach (var incomingProduct in existingProducts)
                {
                    if (dbProductsDict.TryGetValue(incomingProduct.ProductUrl, out var dbProduct))
                    {
                        UpdateVariants(dbProduct, incomingProduct);
                        dbProduct.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

           
            if (newProducts.Any())
            {
                var sdataEntitiesToAdd = new List<Sdata>();
                foreach (var newProduct in newProducts)
                {
                    sdataEntitiesToAdd.Add(MapToNewSdata(newProduct, scraperId));
                }
             
                await _context.Sdata.AddRangeAsync(sdataEntitiesToAdd);
            }

          
            await _context.SaveChangesAsync();
        }

        private void UpdateVariants(Sdata dbProduct, ShopifyFlatProduct incomingProduct)
        {
          
            var incomingVariantsDict = incomingProduct.Variants
                .GroupBy(v => v.Size ?? "")
                .Select(g => g.First())
                .ToDictionary(v => v.Size ?? "");

            foreach (var dbVariant in dbProduct.Variants)
            {
               
                if (incomingVariantsDict.TryGetValue(dbVariant.Size, out var incomingVariant))
                {
                    dbVariant.InStock = incomingVariant.Available == 1;
                    dbVariant.Price = incomingVariant.Price;
                }
            }
        }

        private Sdata MapToNewSdata(ShopifyFlatProduct flatProduct, Guid scraperId)
        {
            return new Sdata
            {
                Uid = _userId,
                Sid = scraperId,
                Title = flatProduct.Title,
                Brand = flatProduct.Brand ?? "",
                Description = flatProduct.Description ?? "",
                ProductUrl = flatProduct.ProductUrl ?? "",
                Price = flatProduct.Price.HasValue ? (int)flatProduct.Price.Value : 0,
                Retail_Price=flatProduct.Retail_Price.HasValue?(int)flatProduct.Retail_Price:0,
                Category = flatProduct.Category ?? "",
                Condition = flatProduct.Condition ??"",
                ConditionGrade = flatProduct.ConditionGrade ??"",
                ProductType = flatProduct.ProductType ?? "",
                Gender = flatProduct.Gender ?? "",
                ScraperName = flatProduct.ScraperName ?? "",
                Status = flatProduct.Status ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Image = flatProduct.Images.Select(img => new ProductImageRecord
                {
                    Priority = img.Priority,
                    Url = img.Url
                }).ToList(),
                Variants = flatProduct.Variants.Select(variant => new ProductVariantRecord
                {
                    Size = variant.Size ?? "",
                    SKU = variant.SKU ?? "",
                    Price = variant.Price,
                    InStock = variant.Available == 1
                }).ToList(),
                Enriched= flatProduct.Enriched,
            };
        }

        
        public Task Update(List<ShopifyFlatProduct> data)
        {
            return Task.CompletedTask;
        }

        public async Task<Dictionary<string, Sdata>> Giveliveproduct(List<ShopifyFlatProduct> existingProducts)
        {
            var productUrls = existingProducts.Select(p => p.ProductUrl).ToList();
            var dbProductsDict = await _context.Sdata
            .Include(s => s.Variants)
            .Where(s => productUrls.Contains(s.ProductUrl) && s.Status=="Live")
            .ToDictionaryAsync(s => s.ProductUrl);

            return dbProductsDict;
        }
        public async Task<Dictionary<string, Sdata>> Giveliveproductperstore(List<ShopifyFlatProduct> existingProducts,Guid storeid)
        {
            var productUrls = existingProducts.Select(p => p.ProductUrl).ToList();
            var dbProductsDict = await _context.Sdata
                .AsNoTracking()
                .Include(s => s.Variants)
                .Include(s => s.ProductStoreMapping.Where(m => m.ShopifyStore.Id == storeid))  // Filter mappings
                .Where(s => productUrls.Contains(s.ProductUrl) 
                            && s.Status == "Live"
                            && s.ProductStoreMapping.Any(m => m.ShopifyStore.Id == storeid))  // Has mapping for this store
                .ToDictionaryAsync(s => s.ProductUrl);

            return dbProductsDict;
        }
        public async Task<List<Sdata>> GiveBulkliveproductperstore(Guid storeid)
        {
            var dbProductsDict =  await _context.Sdata
                .AsNoTracking()
                .Include(s => s.Variants)
                .Include(s => s.Image)
                // Don't include ProductStoreMapping - we don't need it for products NOT on this store
                .Where(s => s.Status == "Live" 
                            && !s.ProductStoreMapping.Any(m => m.ShopifyStore.Id == storeid)  // NOT on this store
                            && s.Variants.Any(v => v.InStock))  // Has in-stock variants
                .Take(5)
                .ToListAsync();

            return dbProductsDict;
        }
        public async Task<int> GiveBulkliveproductperstoreCount(Guid storeid)
        {

            var dbProductsDict =  await _context.Sdata
                .AsNoTracking()
                .Include(s => s.Variants)
                .Include(s => s.Image)
                // Don't include ProductStoreMapping - we don't need it for products NOT on this store
                .Where(s => s.Status == "Live" 
                            && !s.ProductStoreMapping.Any(m => m.ShopifyStore.Id == storeid)  // NOT on this store
                            && s.Variants.Any(v => v.InStock))  // Has in-stock variants
                .CountAsync();

            return dbProductsDict;
        }
    }
}