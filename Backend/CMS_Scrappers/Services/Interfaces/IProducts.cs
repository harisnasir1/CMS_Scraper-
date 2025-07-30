namespace CMS_Scrappers.Services.Interfaces
{
    public interface IProducts
    {
        Task <List<Sdata>> Get_Ready_to_review_products (Guid id, int PageNumber, int PageSize);
        Task<List<Sdata>> pendingReviewproducts( int PageNumber, int PageSize);
    }
}
