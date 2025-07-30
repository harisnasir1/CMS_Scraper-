namespace CMS_Scrappers.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Sdata>> GiveProducts(Guid scrapper,int PageNumber,int PageSize);
        Task<List<Sdata>> GetPendingReviewproducts(int PageNumber,int PageSize);
    }
}
