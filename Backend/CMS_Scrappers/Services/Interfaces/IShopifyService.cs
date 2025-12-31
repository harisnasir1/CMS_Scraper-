namespace CMS_Scrappers.Services.Interfaces
{
    public interface IShopifyService
    {
        Task<string> PushProductAsync(Sdata sdata);
        Task UpdateProduct(List<ShopifyFlatProduct> existingproduct, Dictionary<string, Sdata> sdata);
        Task<bool> Bulk_mutation_shopify_product_creation(List<Sdata> data, string name);
    }
}
