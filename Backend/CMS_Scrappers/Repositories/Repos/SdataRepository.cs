namespace CMS_Scrappers.Repositories.Repos
{
    public class SdataRepository:ISdataRepository
    {
        private readonly Guid _userId;
       
         private readonly AppDbContext _context;

        public SdataRepository(AppDbContext context) {
        _userId= new Guid("0b651c37-c448-42cd-a06e-e01144285502");
        _context = context;
        }
    
         public async Task Add(List<ShopifyFlatProduct> data,Guid _scraperId)
        {
            foreach (var flatProduct in data)
            {
                if (flatProduct.New == false) continue;
                var sdata = new Sdata
                {
                    Uid = _userId,
                    Sid = _scraperId,
                    Title = flatProduct.Title,
                    Brand = flatProduct.Brand ?? "",
                    Description = flatProduct.Description ?? "",
                    ProductUrl = flatProduct.ProductUrl ?? "",
                    Price = flatProduct.Price.HasValue ? (int)flatProduct.Price.Value : 0,
                    Category = flatProduct.Category ?? "",
                    ProductType = flatProduct.ProductType ??"",
                    Gender = flatProduct.Gender ?? "",
                    ScraperName = flatProduct.ScraperName ?? "",
                    Status = flatProduct.Status ?? "",
                    StatusDulicateId = flatProduct.StatusDulicateId ?? "",
                    DuplicateSource = flatProduct.DuplicateSource ?? "",
                    Hashimg = flatProduct.Hashimg ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Image = flatProduct.Images.Select(img => new ProductImageRecord
                    {
                        ProductId = Guid.NewGuid(),
                        Priority = img.Priority,
                        Url = img.Url
                    }).ToList(),
                    Variants = flatProduct.Variants.Select(variant => new ProductVariantRecord
                    {
                        ProductId = Guid.NewGuid(),
                        Size = variant.Size ?? "",
                        SKU = variant.SKU ?? "",
                        Price = variant.Price,
                        InStock = variant.Available == 1 ? true : false
                    }).ToList()
                };

                await _context.Sdata.AddAsync(sdata);
            }

            await _context.SaveChangesAsync();
        }
        public Task Update(List<ShopifyFlatProduct> data)
        {
            return Task.CompletedTask;
        }
    }
}
