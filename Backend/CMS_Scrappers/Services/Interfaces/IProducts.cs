using CMS_Scrappers.Data.Responses.Api_responses;

namespace CMS_Scrappers.Services.Interfaces
{
    public interface IProducts
    {
        Task <List<Sdata>> Get_Ready_to_review_products (Guid id, int PageNumber, int PageSize);
        Task<List<Sdata>> pendingReviewproducts( int PageNumber, int PageSize);
        Task<List<Sdata>> Livefeedproducts(int PageNumber, int PageSize);
        Task RemovingBackgroundimages(Guid id,List<Requestimages>Images);
        Task<ApiResponse<object>> GetSimilarimages(Guid ProductId,int start);
        Task<string> AIGeneratedDescription(Guid id);
        Task<bool> PushProductShopify(Guid id);
        Task<bool> UpdateStatus(Guid id, string status);
        Task<bool> UpdateProductDetails(Guid id, string sku, string title, string description, int price);
        Task<int> ProductCountStatus(string status);

        Task<string> GetProductStatus(Guid id);
    }
}
