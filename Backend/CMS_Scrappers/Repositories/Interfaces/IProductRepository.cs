namespace CMS_Scrappers.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Sdata>> GiveProducts(Guid scrapper,int PageNumber,int PageSize);
        Task<List<Sdata>> GetPendingReviewproducts(int PageNumber,int PageSize);
        Task UpdateImages(Guid id,List<ProductImageRecordDTO> updatedImages);
        Task<Sdata> Getproductbyid(Guid productid);
        Task UpdateDescription(Guid id, string desc);
        Task<bool> AddShopifyproductid(Sdata data, string Shopifyid);
        Task<bool> UpdateStatus(Guid id, string status);
        Task<bool> UpdateProductDetailsAsync(Guid id, string sku, string title, string description, int price);
        Task<int> TotalStatusProdcuts(string status);
        Task<List<Sdata>> GetLiveproducts(int PageNumber, int PageSize);
    }
}
