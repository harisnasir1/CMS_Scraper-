
    public interface ISdataRepository
    {
        Task Add(List<ShopifyFlatProduct> data, Guid _scraperId);
        Task Update(List<ShopifyFlatProduct> data);
        Task<Dictionary<string, Sdata>> Giveliveproduct(List<ShopifyFlatProduct> existingProducts);
    }

