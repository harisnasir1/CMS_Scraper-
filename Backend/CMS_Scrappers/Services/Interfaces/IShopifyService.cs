namespace CMS_Scrappers.Services.Interfaces
{
    public interface IShopifyService
    {
        Task<string> PushProductAsync(Sdata sdata);
        Task UpdateProduct(List<ShopifyFlatProduct> existingproduct, Dictionary<string, Sdata> sdata);
    }
}
