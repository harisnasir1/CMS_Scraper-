using CMS_Scrappers.Ai;
using CMS_Scrappers.Data.Responses.Api_responses;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
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
        private readonly HttpClient _httpClient;
        private readonly S3Interface _S3service;
        private readonly IAi _Ai;
        private readonly IShopifyService _shopifyService;
        public ProductsService(IScrapperRepository scrapperRepository,
            IProductRepository repository, ILogger<ProductsService> logger,
            IGoogleImageService googleservice,
            BackgroundRemover backgroundRemover, HttpClient httpClient, S3Interface s3service,
            IAi Ai,
            IShopifyService shopifyService
            )
        {
            _repository = repository;
            _googleImageService = googleservice;
            _scrapperRepository = scrapperRepository;
            _logger = logger;
            _backgroundRemover = backgroundRemover;
            _httpClient = httpClient;
            _S3service = s3service;
            _Ai = Ai;
            _shopifyService = shopifyService;
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
        public async Task<List<Sdata>> Livefeedproducts(int PageNumber, int PageSize)
        {
            var data = await _repository.GetLiveproducts(PageNumber, PageSize);
            return data;
        }
        public async Task<ApiResponse<Object>> GetSimilarimages(Guid ProductId, int start)
        {
            var data = await _repository.Getproductbyid(ProductId);
            string query = $"{data.Brand + data.Title}";
            return await _googleImageService.SearchImagesAsync(query, start);
        }
        public async Task RemovingBackgroundimages(Guid id, List<Requestimages> Imgs)
        {
            if (Imgs == null || Imgs.Count == 0) return;

            var existingData = await _repository.Getproductbyid(id);
            if (existingData == null) return;

            var processedImages = new List<ProductImageRecordDTO>();

            foreach (var image in Imgs)
            {
                try
                {
                    Stream processedStream;

                    if (image.Bgremove == true)
                    {
                        processedStream = await _backgroundRemover.RemoveBackgroundAsync(image.Url);
                    }
                    else
                    {
                        var response = await _httpClient.GetAsync(image.Url);
                        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            _logger.LogWarning($"Image download failed: {image.Url}! skipping it_");
                            continue; 
                        }
                        processedStream = await response.Content.ReadAsStreamAsync();
                    }

                    using var resizedImage = await _backgroundRemover.ResizeImageAsync(processedStream, 2048, 2048,40);
                    if (resizedImage == null || resizedImage.Length == 0)
                    {
                        _logger.LogWarning($"Resizing failed: {image.Url} skipping it_");
                        continue;
                    }

                    var finalUrl = await _S3service.Uploadimage(resizedImage);
                    if (string.IsNullOrEmpty(finalUrl))
                    {
                        _logger.LogWarning($"S3 upload failed: {image.Url}");
                        continue;
                    }

                    processedImages.Add(new ProductImageRecordDTO
                    {
                        Id = image.Id.ToString(),
                        Priority = image.Priority,
                        Url = finalUrl,
                        Bgremove = image.Bgremove
                    });

                    await Task.Delay(900); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Image processing failed: {image.Url}");
                }
            }
            if (processedImages.Count > 0)
            {
                await _repository.UpdateImages(id, processedImages);
            }
        }

        public async Task<string> AIGeneratedDescription(Guid id)
        {
            _logger.LogCritical("comming for ai description");
            return await _Ai.GenerateDescription(id);
        }

        public async Task <bool> PushProductShopify(Guid id)
        {
            var data=await _repository.Getproductbyid(id);
            if (data == null) return false;
            string response=await _shopifyService.PushProductAsync(data);
          
            var updated = await _repository.AddShopifyproductid(data, response);
          
            return true;
        }
        public async Task<bool> UpdateStatus(Guid id , string status)
        {
            return await _repository.UpdateStatus(id, status);
        }
        public async Task<bool> UpdateProductDetails(Guid id,string sku, string title, string description, int price)
        {
            return await _repository.UpdateProductDetailsAsync(id, sku, title, description, price);
        }

        public async Task<int>ProductCountStatus(string status)
        {
            return await _repository.TotalStatusProdcuts(status);
        }
        
    }
}