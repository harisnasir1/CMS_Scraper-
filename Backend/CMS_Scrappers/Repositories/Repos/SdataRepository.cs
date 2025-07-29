using Microsoft.EntityFrameworkCore;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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
            // 1. Separate products into new and existing lists
            var newProducts = data.Where(p => p.New).ToList();
            var existingProducts = data.Where(p => !p.New).ToList();

            // 2. Handle existing products in bulk
            if (existingProducts.Any())
            {
                // Fetch all relevant products from the DB in a SINGLE query.
                // Using a dictionary provides fast O(1) lookups.
                var productUrls = existingProducts.Select(p => p.ProductUrl).ToList();
                var dbProductsDict = await _context.Sdata
                    .Include(s => s.Variants)
                    .Where(s => productUrls.Contains(s.ProductUrl))
                    .ToDictionaryAsync(s => s.ProductUrl);

                foreach (var incomingProduct in existingProducts)
                {
                    // Find the matching product from our dictionary
                    if (dbProductsDict.TryGetValue(incomingProduct.ProductUrl, out var dbProduct))
                    {
                        // Update the product's variants efficiently
                        UpdateVariants(dbProduct, incomingProduct);
                        dbProduct.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // 3. Handle new products in bulk
            if (newProducts.Any())
            {
                var sdataEntitiesToAdd = new List<Sdata>();
                foreach (var newProduct in newProducts)
                {
                    sdataEntitiesToAdd.Add(MapToNewSdata(newProduct, scraperId));
                }
                // Add all new entities to the context at once.
                await _context.Sdata.AddRangeAsync(sdataEntitiesToAdd);
            }

            // 4. Save all changes (updates and inserts) to the database in ONE transaction.
            await _context.SaveChangesAsync();
        }

        private void UpdateVariants(Sdata dbProduct, ShopifyFlatProduct incomingProduct)
        {
            // To handle duplicate sizes, we first group by the Size.
            // Then, we select the *first* item from each group.
            // This effectively ignores any subsequent variants with the same size.
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
                Category = flatProduct.Category ?? "",
                Condition = flatProduct.Condition ??"",
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
    }
}