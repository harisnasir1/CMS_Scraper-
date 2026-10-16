using System.Threading.Tasks.Dataflow;
using CMS_Scrappers.Ai;
using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Data.Responses.Api_responses;
using CMS_Scrappers.Models;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
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
        private readonly IProductSyncCoordinator _productSyncCoordinator;
        private readonly IProductStoreMappingRepository _productStoreMappingRepository;
        private readonly AutoApproveSettings _autoApprove;
        private readonly IRRSyncService _rrSyncService;
        private readonly IRRSyncProductMapRepository _rrSyncProductMapRepository;
        public ProductsService(IScrapperRepository scrapperRepository,
            IProductRepository repository, ILogger<ProductsService> logger,
            IGoogleImageService googleservice,
            BackgroundRemover backgroundRemover, HttpClient httpClient, S3Interface s3service,
            IAi Ai,
            IProductStoreMappingRepository productStoreMappingRepository,
            IProductSyncCoordinator productsyncCoordinator,
            AutoApproveSettings autoApprove,
            IRRSyncService rrSyncService,
            IRRSyncProductMapRepository rrSyncProductMapRepository
            )
        {
            _autoApprove = autoApprove;
            _rrSyncService = rrSyncService;
            _rrSyncProductMapRepository = rrSyncProductMapRepository;
            _repository = repository;
            _googleImageService = googleservice;
            _scrapperRepository = scrapperRepository;
            _logger = logger;
            _backgroundRemover = backgroundRemover;
            _httpClient = httpClient;
            _S3service = s3service;
            _Ai = Ai;
            _productSyncCoordinator = productsyncCoordinator;
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
        public async Task<int> RemovingBackgroundimages(Guid id, List<Requestimages> Imgs)
        {
            if (Imgs == null || Imgs.Count == 0)
            {
                Console.WriteLine($"[RAILWAY_DEBUG] No images to process for product {id}");
                return 0;
            }

            var existingData = await _repository.Getproductbyid(id);
            if (existingData == null)
            {
                Console.WriteLine($"[RAILWAY_DEBUG] Product {id} not found in database");
                return 0;
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
                    var saved = await _repository.UpdateImages(id, processedImages);
                    if (!saved)
                    {
                        _logger.LogError($"Failed to update images in database for product {id}");
                        return 0;
                    }
                    Console.WriteLine($"[RAILWAY_SUCCESS] Database update successful for product {id}");
                    _logger.LogInformation($"Successfully updated {processedImages.Count} images for product {id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RAILWAY_ERROR] Database update failed for product {id}: {ex.Message}");
                    Console.WriteLine($"[RAILWAY_ERROR] Database stack trace: {ex.StackTrace}");
                    _logger.LogError(ex, $"Failed to update images in database for product {id}: {ex.Message}");
                    return 0;
                }
            }
            else
            {
                Console.WriteLine($"[RAILWAY_WARNING] No images were successfully processed for product {id}");
                _logger.LogWarning($"No images were successfully processed for product {id}");
            }

            return successCount;
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
            
            var status = await _productSyncCoordinator.pushProductslive(data);
        
          
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

        public Task<List<Guid>> GetAutoApproveCandidates(Guid sid, int? limit)
        {
            return _repository.GiveAutoApproveCandidateIds(
                sid,
                limit,
                _autoApprove.MinVariantPrice,
                _autoApprove.ExcludedBrands,
                _autoApprove.MinImages);
        }

        public async Task<bool> AutoApproveProduct(Guid id)
        {
            // Claim the product so a concurrent manual Push cannot grab it mid-way.
            if (!await _repository.UpdateStatus(id, "Sync_ready"))
            {
                _logger.LogInformation("AutoApprove: {Id} is no longer Categorized, skipping", id);
                return false;
            }

            try
            {
                var data = await _repository.Getproductbyid(id);
                if (data == null)
                {
                    _logger.LogWarning("AutoApprove: {Id} not found", id);
                    return false;
                }

                string description;
                try
                {
                    description = await _Ai.GenerateDescription(id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AutoApprove: AI description failed for {Id}, leaving in review", id);
                    await RevertToReview(id);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(description) || description.Length < CMS_Scrappers.Ai.Implementation.AI.MinDescriptionLength)
                {
                    _logger.LogWarning("AutoApprove: AI description too short ({Length} chars) for {Id}, leaving in review", description?.Length ?? 0, id);
                    await RevertToReview(id);
                    return false;
                }

                if (!await _repository.UpdateProductDetailsAsync(id, Gen_Sku(data.Brand), data.Title, description, data.Price))
                {
                    _logger.LogWarning("AutoApprove: could not save details for {Id}, leaving in review", id);
                    await RevertToReview(id);
                    return false;
                }

                var (keep, drop) = BuildImagePlan(data.Image);

                var processed = await RemovingBackgroundimages(id, keep);
                if (processed == 0)
                {
                    _logger.LogWarning("AutoApprove: no images processed for {Id}, leaving in review", id);
                    await RevertToReview(id);
                    return false;
                }

                if (drop.Count > 0)
                {
                    var deleted = await _repository.DeleteImages(id, drop);
                    _logger.LogInformation("AutoApprove: dropped {Deleted} trailing images for {Id}", deleted, id);
                }

                if (!await _repository.UpdateStatus(id, "Live"))
                {
                    _logger.LogError("AutoApprove: could not set Live for {Id}", id);
                    await RevertToReview(id);
                    return false;
                }

                _logger.LogInformation("AutoApprove: {Id} is Live ({Kept} images, {BgRemoved} bg-removed, {Dropped} dropped)",
                    id, keep.Count, keep.Count(k => k.Bgremove == true), drop.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoApprove: unexpected failure for {Id}, leaving in review", id);
                await RevertToReview(id);
                return false;
            }
        }

        private async Task RevertToReview(Guid id)
        {
            try
            {
                await _repository.UpdateStatus(id, "Categorized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoApprove: failed to revert {Id} to Categorized", id);
            }
        }

        /// <summary>
        /// Splits a product's images into the ones to keep (with bg-removal flags) and the
        /// trailing ones to delete, according to the configured image tiers.
        /// Images are ordered by their scraped priority, so "last" means the closeups.
        /// </summary>
        private (List<Requestimages> keep, List<long> drop) BuildImagePlan(List<ProductImageRecord> images)
        {
            var ordered = images
                .OrderBy(i => i.Priority ?? int.MaxValue)
                .ThenBy(i => i.Id)
                .ToList();

            int total = ordered.Count;
            var tier = _autoApprove.TierFor(total);

            // Never drop below one image.
            int dropCount = Math.Clamp(tier.DropLast, 0, Math.Max(0, total - 1));
            int keepCount = total - dropCount;
            int bgCount = Math.Clamp(tier.BgRemove, 0, keepCount);

            var keep = new List<Requestimages>();
            for (int k = 0; k < keepCount; k++)
            {
                keep.Add(new Requestimages
                {
                    Id = ordered[k].Id,
                    Url = ordered[k].Url,
                    Priority = k,
                    Bgremove = k < bgCount,
                });
            }

            var drop = ordered.Skip(keepCount).Select(i => i.Id).ToList();
            return (keep, drop);
        }

        public async Task<DeleteProductResult> DeleteLiveProduct(Guid id)
        {
            var result = new DeleteProductResult();

            var product = await _repository.Getproductbyid(id);
            if (product == null)
            {
                result.Message = "Product not found";
                return result;
            }
            if (product.Status != "Live")
            {
                result.Message = $"Product is '{product.Status}', only Live products can be deleted this way";
                return result;
            }

            _logger.LogInformation("DeleteLiveProduct: starting for {Id} ({Url})", id, product.ProductUrl);

           var rrMap = await _rrSyncProductMapRepository.Get(id);
            if (rrMap != null && rrMap.Id != Guid.Empty)
            {
                result.HadRRSyncMapping = true;
                var rr = await _rrSyncService.DeleteProductAsync(rrMap.RRSyncProductId);
                if (!rr.IsSuccess)
                {
                    result.Message = $"RRSync delete failed, nothing was deleted: {rr.Message}";
                    _logger.LogError("DeleteLiveProduct: RRSync delete failed for {Id} (rr {RrId}): {Msg}", id, rrMap.RRSyncProductId, rr.Message);
                    return result;
                }
                result.RRSyncDeleted = true;
                _logger.LogInformation("DeleteLiveProduct: RRSync product {RrId} deleted for {Id} ({Status})", rrMap.RRSyncProductId, id, rr.Status);
            }
            else
            {
                result.RRSyncDeleted = true; // nothing to remove
            }

           
            var (deleted, failed) = await _productSyncCoordinator.DeleteProductFromAllStores(id);
            result.ShopifyStoresDeleted = deleted;
            result.ShopifyStoresFailed = failed;
            if (failed > 0)
            {
                _logger.LogWarning("DeleteLiveProduct: {Failed} store(s) still have product {Id}; their mappings were kept but the local row is being removed", failed, id);
            }
            
            result.LocalDeleted = await _repository.DeleteProduct(id);
            if (!result.LocalDeleted)
            {
                result.Message = "Product vanished before local delete";
                return result;
            }

            result.Success = true;
            result.Message = failed > 0
                ? $"Deleted. {failed} Shopify store(s) failed and need manual cleanup. Scraper will re-add on its next run."
                : "Deleted everywhere. Scraper will re-add on its next run.";
            _logger.LogInformation("DeleteLiveProduct: done for {Id} (rrsync={Rr}, stores deleted={Del}, failed={Fail})",
                id, result.RRSyncDeleted, deleted, failed);
            return result;
        }

        public async Task<bool> shiftallshopifyidstonew()
        {
            var livedata = await this.Livefeedproducts(1,6000);
            Guid originalStoreId;
            bool isValid = Guid.TryParse("", out originalStoreId);

            if (!isValid)
            {
                _logger.LogInformation("No valid store id please add store id!");
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
            
           
        } //migration script 

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