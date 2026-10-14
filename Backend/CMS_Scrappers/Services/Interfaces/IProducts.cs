using CMS_Scrappers.Data.Responses.Api_responses;

namespace CMS_Scrappers.Services.Interfaces
{
    public interface IProducts
    {
        Task <List<Sdata>> Get_Ready_to_review_products (Guid id, int PageNumber, int PageSize);
        Task<List<Sdata>> pendingReviewproducts( int PageNumber, int PageSize);
        Task<List<Sdata>> Livefeedproducts(int PageNumber, int PageSize);
        /// <summary>Processes the given images (bg removal / resize / S3) and returns how many succeeded.</summary>
        Task<int> RemovingBackgroundimages(Guid id,List<Requestimages>Images);
        Task<ApiResponse<object>> GetSimilarimages(Guid ProductId,int start);
        Task<string> AIGeneratedDescription(Guid id);
        Task<bool> PushProductShopify(Guid id);
        Task<bool> UpdateStatus(Guid id, string status);
        Task<bool> UpdateProductDetails(Guid id, string sku, string title, string description, int price);
        Task<int> ProductCountStatus(string status);
        Task<int> product_Count_per_Scarpper(Guid id);
        Task<string> GetProductStatus(Guid id);

        /// <summary>Ids of "Categorized" products that pass the AutoApproveSettings filters, oldest first.</summary>
        Task<List<Guid>> GetAutoApproveCandidates(Guid sid, int? limit);

        /// <summary>
        /// Promotes one "Categorized" product to "Live" without manual review:
        /// AI description, SKU, trailing-image removal and background removal per AutoApproveSettings.
        /// On any failure the product is put back to "Categorized". Returns true if it went Live.
        /// </summary>
        Task<bool> AutoApproveProduct(Guid id);

        Task<bool> shiftallshopifyidstonew();
        
      
    } 
}
