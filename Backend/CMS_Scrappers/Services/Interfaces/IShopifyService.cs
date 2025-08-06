namespace CMS_Scrappers.Services.Interfaces
{
    public interface IShopifyService
    {
        Task<string> PushProductAsync(Sdata sdata);
    }
}
