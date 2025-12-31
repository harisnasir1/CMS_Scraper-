
    public interface ISdataRepository
    {
        Task Add(List<ShopifyFlatProduct> data, Guid _scraperId);
        Task Update(List<ShopifyFlatProduct> data);
        Task<Dictionary<string, Sdata>> Giveliveproduct(List<ShopifyFlatProduct> existingProducts);
        
        Task<Dictionary<string, Sdata>> Giveliveproductperstore(List<ShopifyFlatProduct> existingProducts , Guid storeid);

        Task<List<Sdata>> GiveBulkliveproductperstore(Guid storeid);
    }

