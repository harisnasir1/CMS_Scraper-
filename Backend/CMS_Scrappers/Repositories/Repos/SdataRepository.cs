using Microsoft.EntityFrameworkCore;

using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Amazon.S3.Model;
using CMS_Scrappers.Data.DTO;

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
                       var change= UpdateVariants(dbProduct, incomingProduct);
                       dbProduct.LastViewed = DateTime.UtcNow;
                       if(change)
                       {
                           dbProduct.UpdatedAt = DateTime.UtcNow;
                       }
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

            // Pass 3: Mark unseen products' variants as out of stock
            var allScrapedUrls = data.Select(p => p.ProductUrl).ToHashSet();
            var unseenProducts = await _context.Sdata
                .Include(s => s.Variants)
                .Where(s => s.Sid == scraperId
                            && !allScrapedUrls.Contains(s.ProductUrl))
                .ToListAsync();

            foreach (var product in unseenProducts)
            {
                foreach (var variant in product.Variants)
                {
                    if (variant.InStock)
                    {
                        variant.InStock = false;
                        variant.UpdatedAt = DateTime.UtcNow;
                    }
                }
                product.Status = "SourceDeleted";
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private bool UpdateVariants(Sdata dbProduct, ShopifyFlatProduct incomingProduct)
        {
            var change = false;
            var incomingVariantsDict = incomingProduct.Variants
                .GroupBy(v => v.Size ?? "")
                .Select(g => g.First())
                .ToDictionary(v => v.Size ?? "");
            var existingSizes = new HashSet<string>();
            foreach (var dbVariant in dbProduct.Variants)
            {
                existingSizes.Add(dbVariant.Size);
                if (incomingVariantsDict.TryGetValue(dbVariant.Size, out var incomingVariant))
                {
                    var newInStock = incomingVariant.Available == 1;
                    var newPrice = incomingVariant.Price;
                    dbVariant.LastViewed = DateTime.UtcNow;
                    // Only bump UpdatedAt if something actually changed
                    if (dbVariant.InStock != newInStock || dbVariant.Price != newPrice)
                    {
                        dbVariant.InStock = newInStock;
                        dbVariant.Price = newPrice;
                        dbVariant.UpdatedAt = DateTime.UtcNow;
                        change = true;
                    }
                }
                else
                {
                    // Variant no longer on supplier — mark out of stock immediately
                    if (dbVariant.InStock)
                    {
                        dbVariant.InStock = false;
                        dbVariant.UpdatedAt = DateTime.UtcNow;
                        change = true;
                    }
                   
                }
                
            }
            foreach (var (size, incoming) in incomingVariantsDict)
            {
                if (existingSizes.Contains(size)) continue;

                dbProduct.Variants.Add(new ProductVariantRecord
                {
                    SdataId = dbProduct.Id,
                    Size = size,
                    SKU = incoming.SKU ?? "",
                    Price = incoming.Price,
                    InStock = incoming.Available == 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastViewed = DateTime.UtcNow
                });
                change = true;
            }
            return change;
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
                LastViewed = DateTime.UtcNow,
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
                    InStock = variant.Available == 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastViewed = DateTime.UtcNow,
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

      public async Task<Dictionary<string,Sdata>> GiveLiveDataToSync(DateTime scrapeStartedAt)
        {
            return await _context.Sdata
                .AsNoTracking()
                .Include(s => s.Variants)
                .Include(s => s.Image)
                .Where(s => s.Status == "Live"&& s.Description != "")
                .Where(s =>
                    // No map at all — never synced
                    (!_context.RRSyncProductMap.Any(m => m.SdataId == s.Id) 
                     && s.Variants.Any(v => v.InStock))
                    // OR a variant has changed since its map was last touched
                    || s.Variants.Any(v =>
                        _context.RRSyncVariantMap.Any(m =>
                            m.VariantId == v.Id
                            && m.SyncStatus == "Active"
                            && m.UpdatedAt < v.UpdatedAt))
                    // OR Sdata itself has changed since the product map was touched
                    || _context.RRSyncProductMap.Any(m =>
                        m.SdataId == s.Id
                        && m.SyncStatus == "Active"
                        && m.UpdatedAt < s.UpdatedAt))
                .OrderBy(s => s.Id)
                .AsSplitQuery()
                .ToDictionaryAsync(s => s.Id.ToString(), s => s);
        }

      public async  Task<List<StaleVariantInfo>> GiveStaleVariants(Guid shopifyStoreId, DateTime threshold)
        {
            return await (
                    from vsm in _context.VariantStoreMapping
                    join psm in _context.ProductStoreMapping 
                        on vsm.ProductStoreMappingId equals psm.Id
                    join v in _context.ProductVariants 
                        on vsm.VariantId equals v.Id
                    where psm.ShopifyStoreId == shopifyStoreId
                          && (v.LastViewed == null || v.LastViewed < threshold)
                    select new StaleVariantInfo()
                    {
                        VariantId          = v.Id,
                        ShopifyVariantId   = vsm.ShopifyVariantId,
                        ShopifyProductId   = psm.ExternalProductId,  
                        ProductStoreMappingId = psm.Id,
                        VariantStoreMappingId = vsm.Id,
                        SdataId            = psm.ProductId
                    })
                .ToListAsync();
        }

        public async Task DelunseenData(Guid scraperId, DateTime threshold)
        {
            try
            {
                var unseenProducts = await _context.Sdata
                    .Include(s => s.Variants)
                    .Where(s => s.Sid == scraperId
                                && s.Status != "SourceDeleted"
                                && s.Status != "Delisted"
                                && (s.LastViewed == null || s.LastViewed < threshold))
                    .ToListAsync();

                if (!unseenProducts.Any())
                {
                  
                    return;
                }

                var markedCount = 0;
                var variantCount = 0;
                foreach (var product in unseenProducts)
                {
                    foreach (var variant in product.Variants)
                    {
                        if (variant.InStock)
                        {
                            variant.InStock = false;
                            variant.UpdatedAt = DateTime.UtcNow;
                            variantCount++;
                        }
                    }
                    product.Status = "SourceDeleted";
                    product.UpdatedAt = DateTime.UtcNow;
                    markedCount++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}