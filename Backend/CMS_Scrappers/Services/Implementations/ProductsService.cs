using System.Threading.Tasks.Dataflow;
using CMS_Scrappers.Ai;
using CMS_Scrappers.Data.Responses.Api_responses;
using CMS_Scrappers.Models;
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
        private readonly IProductStoreMappingRepository _productStoreMappingRepository;
        public ProductsService(IScrapperRepository scrapperRepository,
            IProductRepository repository, ILogger<ProductsService> logger,
            IGoogleImageService googleservice,
            BackgroundRemover backgroundRemover, HttpClient httpClient, S3Interface s3service,
            IAi Ai,
            IShopifyService shopifyService,
            IProductStoreMappingRepository productStoreMappingRepository
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
            _productStoreMappingRepository = productStoreMappingRepository;
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

                    bool fill = image.Bgremove==false ? true : false;
                    Console.WriteLine($"[RAILWAY_DEBUG] Resizing image {i + 1}: {image.Url} (fill: {fill})");

                    using var resizedImage = await _backgroundRemover.ResizeImageAsync(processedStream, 2048, 2048, 10, fill);
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
            if(response == null) return false;
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
        
        public async Task<string> GetProductStatus(Guid id)
        {
            var product = await _repository.Getproductbyid(id);
            return product?.Status ?? "Unknown";
        }
        public async Task<int> product_Count_per_Scarpper(Guid id)
        {
            return await _repository.GiveProducts_Count(id);
        }

        public async Task<bool> PushAllScraperProductsLive(Guid sid,int? limit)
        {
            //step 1 to check if any scrapper is running or syncing. can do later have to do . make scrapper running if product is syncing.
            
            //step 2 to get the count of categorized product for each scraper.
            int batch_size = 1;
            int total_d = limit ?? (await _repository.GiveProducts_Count(sid));

            int Spcount = (int)Math.Ceiling(total_d / (double)batch_size);
         
            for (int i = 0; i < Spcount; i++)
            {
                try 
                {
                    var sdata = await _repository.GiveInstockProducts(sid, i+1, batch_size);
                    _logger.LogInformation($"Successfully retrieved {sdata.Count} products");
                    foreach (var data in sdata)
                    {
                        var ai_des=await _Ai.GenerateDescription(data.Id);
                        await _repository.UpdateProductDetailsAsync(data.Id,  Gen_Sku(data.Brand), data.Title, ai_des, data.Price);
                    
                        var images = new List<Requestimages>();
                        var allImages = data.Image.ToList();
                        int total = allImages.Count;

                        int removeCount = 0;
                    
                        if (total > 6)
                        {
                            removeCount = 3;
                        }
                        else if (total > 5)  
                        {
                            removeCount = 2;
                        }
                        else if (total > 1)  
                        {
                            removeCount = 1;
                        }
                       

                        for (int k = 0; k < total; k++)
                        {
                            bool bgRemoveValue = k < removeCount;   

                            images.Add(new Requestimages
                            {
                                Id = unchecked((int)allImages[k].Id),
                                Url = allImages[k].Url,
                                Priority=k,
                                Bgremove = bgRemoveValue,
                            });
                        }
                        await this.RemovingBackgroundimages(data.Id, images);
                        await _repository.UpdateStatus(data.Id, "Sync_ready");
                        
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get products. SID: {ScraperId}", sid);
                    throw;
                }
         
            }
            
            return true;
            
        }

        public async Task<bool> shiftallshopifyidstonew()
        {
            var livedata = await this.Livefeedproducts(1,6000);
            Guid originalStoreId;
            bool isValid = Guid.TryParse("0ddc7087-6180-4cb4-8bec-c13fdfe44df3", out originalStoreId);

            if (!isValid)
            {
               return false;
            }
            int migratedCount = 0;
            int skippedCount = 0;

            foreach (var product in livedata)
            {
                // Skip if no Shopifyid
                if (string.IsNullOrEmpty(product.Shopifyid))
                {
                    skippedCount++;
                    continue;
                }

                // Create mapping
                var mapping = new ProductStoreMapping
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ShopifyStoreId = originalStoreId,
                    ExternalProductId = product.Shopifyid,
                    SyncStatus = "Live",
                    LastSyncedAt = product.UpdatedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _productStoreMappingRepository.InsertProductmapping(mapping);
                migratedCount++;

                // Log progress every 50 products
                if (migratedCount % 50 == 0)
                {
                    _logger.LogInformation($"Migrated {migratedCount} products so far...");
                }
            }

            _logger.LogInformation($"Migration complete! Migrated: {migratedCount}, Skipped: {skippedCount}");
            return true;
            
           
        }

        private string Gen_Sku(string brand)
        {
            string prefix = brand?.Substring(0, Math.Min(2, brand.Length)); // safe 2-letter slice

            var random = new Random();
            int number = random.Next(99999, 9999999); // same as JS range

            string full = prefix + number.ToString();

            return full ?? "";
        }



    }
}