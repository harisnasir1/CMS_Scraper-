using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils.Bgremovel;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Query;
namespace CMS_Scrappers.Services.Implementations
{
    public class ProductsService : IProducts
    {
        private readonly IProductRepository _repository;
        private readonly IScrapperRepository _scrapperRepository;
        private readonly ILogger<ProductsService> _logger;
        private readonly IGoogleImageService _googleImageService;
        private readonly BackgroundRemover _backgroundRemover;
        public ProductsService(IScrapperRepository scrapperRepository,
            IProductRepository repository, ILogger<ProductsService> logger,
            IGoogleImageService googleservice,
            BackgroundRemover backgroundRemover)
        {
            _repository = repository;
            _googleImageService = googleservice;
            _scrapperRepository = scrapperRepository;
            _logger = logger;
            _backgroundRemover = backgroundRemover;
        }

        public async Task<List<Sdata>> Get_Ready_to_review_products(Guid id, int PageNumber, int PageSize)
        {
            var data = await _repository.GiveProducts(id, PageNumber, PageSize);
            return data;
        }
        public async Task<List<Sdata>> pendingReviewproducts(int PageNumber, int PageSize)
        {
            var data = await _repository.GetPendingReviewproducts(PageNumber, PageSize);
            return data;
        }
        public async Task<ApiResponse<Object>> GetSimilarimages(Guid ProductId, int start)
        {
            var data = await _repository.Getproductbyid(ProductId);
            string query = $"{data.Brand + data.Title}";
            return await _googleImageService.SearchImagesAsync(query, start);
        }
        public async Task RemovingBackgroundimages(Guid id)
        {
            var data = await _repository.Getproductbyid(id);
            var copiedImages = data.Image.ToList();
            var Images = new List<ProductImageRecord>();
            foreach (var image in copiedImages)
            {
                var resultJson = await _backgroundRemover.RemoveBackgroundAsync(imageUrl: image.Url);
                var json = System.Text.Json.JsonDocument.Parse(resultJson);
                _logger.LogError($"Url: {resultJson}");
                if (json.RootElement.TryGetProperty("url", out var outputUrlElement))
                {
                    string newUrl = outputUrlElement.GetString();
                   
                    image.Url = newUrl;
                    Images.Add(image);
                    
                }
                break;
            }
            if (Images.Count > 0)
            {
                await _repository.UpdateImages(id, Images);
            }
        }
    }
}
