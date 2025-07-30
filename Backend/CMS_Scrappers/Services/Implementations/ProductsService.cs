using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
namespace CMS_Scrappers.Services.Implementations
{
    public class ProductsService:IProducts
    { 
       private readonly IProductRepository _repository;
       private readonly IScrapperRepository _scrapperRepository;
       private readonly ILogger<ProductsService>   _logger;
        public ProductsService(IScrapperRepository scrapperRepository,IProductRepository repository,ILogger<ProductsService> logger) { 
          _repository = repository;
          _scrapperRepository = scrapperRepository;
          _logger = logger;
        }

        public async Task<List<Sdata>> Get_Ready_to_review_products(Guid id, int PageNumber, int PageSize)
        {

            var data = await _repository.GiveProducts(id,PageNumber,PageSize);
            return data;
        }
        public async Task<List<Sdata>> pendingReviewproducts( int PageNumber, int PageSize)
        {

            var data = await _repository.GetPendingReviewproducts( PageNumber, PageSize);
            return data;
        }
        
    }
}
