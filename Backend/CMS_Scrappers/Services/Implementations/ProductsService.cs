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
            if (Imgs == null || Imgs.Count == 0) 
            {
                Console.WriteLine($"[RAILWAY_DEBUG] No images to process for product {id}");
                return;
            }

            var existingData = await _repository.Getproductbyid(id);
            if (existingData == null) 
            {
                Console.WriteLine($"[RAILWAY_DEBUG] Product {id} not found in database");
                return;
            }

            Console.WriteLine($"[RAILWAY_DEBUG] Starting image processing for product {id}. Total images: {Imgs.Count}");
            _logger.LogInformation($"[RAILWAY_DEBUG] Starting image processing for product {id}. Total images: {Imgs.Count}");

            var processedImages = new List<ProductImageRecordDTO>();
            int i = 0;
            int successCount = 0;
            int failureCount = 0;

            foreach (var image in Imgs)
            {
                Stream processedStream = null;
                Console.WriteLine($"[RAILWAY_DEBUG] Processing image {i + 1}/{Imgs.Count}: {image.Url}");
                
                try
                {
                    if (image.Bgremove == true)
                    {
                        Console.WriteLine($"[RAILWAY_DEBUG] Removing background for image: {image.Url}");
                        processedStream = await _backgroundRemover.RemoveBackgroundAsync(image.Url);
                        Console.WriteLine($"[RAILWAY_DEBUG] Background removal completed for: {image.Url}");
                    }
                    else
                    {
                        Console.WriteLine($"[RAILWAY_DEBUG] Downloading image: {image.Url}");
                        var response = await _httpClient.GetAsync(image.Url);
                        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            Console.WriteLine($"[RAILWAY_ERROR] Image download failed: {image.Url} - Status: {response?.StatusCode}");
                            _logger.LogWarning($"Image download failed: {image.Url}! skipping it_");
                            failureCount++;
                            continue; 
                        }
                        processedStream = await response.Content.ReadAsStreamAsync();
                        Console.WriteLine($"[RAILWAY_DEBUG] Image download completed: {image.Url}");
                    }

                    if (processedStream == null)
                    {
                        Console.WriteLine($"[RAILWAY_ERROR] Failed to get image stream for: {image.Url}");
                        _logger.LogWarning($"Failed to get image stream for: {image.Url}");
                        failureCount++;
                        continue;
                    }

                    bool fill = i < 2 ? false : true;
                    Console.WriteLine($"[RAILWAY_DEBUG] Resizing image {i + 1}: {image.Url} (fill: {fill})");

                    using var resizedImage = await _backgroundRemover.ResizeImageAsync(processedStream, 2048, 2048, 40, fill);
                    if (resizedImage == null || resizedImage.Length == 0)
                    {
                        Console.WriteLine($"[RAILWAY_ERROR] Resizing failed: {image.Url}");
                        _logger.LogWarning($"Resizing failed: {image.Url} skipping it_");
                        failureCount++;
                        continue;
                    }

                    Console.WriteLine($"[RAILWAY_DEBUG] Resizing completed. Image size: {resizedImage.Length} bytes");

                    Console.WriteLine($"[RAILWAY_DEBUG] Uploading to S3: {image.Url}");
                    var finalUrl = await _S3service.Uploadimage(resizedImage);
                    if (string.IsNullOrEmpty(finalUrl))
                    {
                        Console.WriteLine($"[RAILWAY_ERROR] S3 upload failed: {image.Url}");
                        _logger.LogWarning($"S3 upload failed: {image.Url}");
                        failureCount++;
                        continue;
                    }

                    Console.WriteLine($"[RAILWAY_DEBUG] S3 upload successful: {finalUrl}");

                    processedImages.Add(new ProductImageRecordDTO
                    {
                        Id = image.Id.ToString(),
                        Priority = image.Priority,
                        Url = finalUrl,
                        Bgremove = image.Bgremove
                    });

                    successCount++;
                    Console.WriteLine($"[RAILWAY_SUCCESS] Image {i + 1}/{Imgs.Count} processed successfully: {image.Url} -> {finalUrl}");
                    _logger.LogInformation($"Successfully processed image {i + 1}/{Imgs.Count}: {image.Url} -> {finalUrl}");
                    
                    await Task.Delay(900);
                    i++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"[RAILWAY_ERROR] Image processing failed for {image.Url}: {ex.Message}");
                    Console.WriteLine($"[RAILWAY_ERROR] Stack trace: {ex.StackTrace}");
                    _logger.LogError(ex, $"Image processing failed for {image.Url}: {ex.Message}");
                }
                finally
                {
                    // Ensure stream is properly disposed
                    if (processedStream != null)
                    {
                        try
                        {
                            processedStream.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Console.WriteLine($"[RAILWAY_WARNING] Failed to dispose stream for {image.Url}: {disposeEx.Message}");
                            _logger.LogWarning($"Failed to dispose stream for {image.Url}: {disposeEx.Message}");
                        }
                    }
                }
            }
            
            Console.WriteLine($"[RAILWAY_SUMMARY] Processing complete for product {id}. Success: {successCount}, Failed: {failureCount}, Total: {Imgs.Count}");
            
            if (processedImages.Count > 0)
            {
                try
                {
                    Console.WriteLine($"[RAILWAY_DEBUG] Updating database with {processedImages.Count} images");
                    await _repository.UpdateImages(id, processedImages);
                    Console.WriteLine($"[RAILWAY_SUCCESS] Database update successful for product {id}");
                    _logger.LogInformation($"Successfully updated {processedImages.Count} images for product {id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RAILWAY_ERROR] Database update failed for product {id}: {ex.Message}");
                    Console.WriteLine($"[RAILWAY_ERROR] Database stack trace: {ex.StackTrace}");
                    _logger.LogError(ex, $"Failed to update images in database for product {id}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[RAILWAY_WARNING] No images were successfully processed for product {id}");
                _logger.LogWarning($"No images were successfully processed for product {id}");
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