namespace CMS_Scrappers.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Sdata>> GiveProducts(Guid scrapper,int PageNumber,int PageSize);
    }
}
