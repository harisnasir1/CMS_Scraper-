using System.Security.Cryptography.Xml;
using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Data.DTO.RR_Sync_DTO;
using CMS_Scrappers.Data.Requests;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CreateRRSyncProductImage = CMS_Scrappers.Data.Requests.CreateRRSyncProductImage;

namespace CMS_Scrappers.Coordinators.Implementations;

public class RRsyncCoordinator : IRRsyncCoordinator
{
    private readonly ISdataRepository _sdataRepository;
    private readonly ILogger<RRsyncCoordinator> _logger;
    private readonly IRRSyncProductMapRepository _productMapRepository;
    private readonly IRRSyncVariantMapRepository _variantMapRepository;
    private readonly IRRSyncService _syncService;
   
    private readonly int RatelimitCount = 250;

    public RRsyncCoordinator(ILogger<RRsyncCoordinator> logger, IRRSyncProductMapRepository productMapRepository,
        IRRSyncVariantMapRepository variantMapRepository, ISdataRepository sdataRepository,
        IRRSyncService syncService)
    {
        _logger = logger;
        _productMapRepository = productMapRepository;
        _variantMapRepository = variantMapRepository;
        _sdataRepository = sdataRepository;
        _syncService = syncService;
        
    }

    public async Task<bool> Syncportal(DateTime scrapeStartedAt,string ScraperName)
    {
        var sourceLookupName = $"cms-{ScraperName}";
        var allliveproducts = await _sdataRepository.GiveLiveDataToSync(scrapeStartedAt);
        var NewData = new List<CreateRRSyncProductRequest>();
        var SourceResponse = await _syncService.GetSourceByname(sourceLookupName);
        if (!SourceResponse.IsSuccess || SourceResponse.Payload == null)
        {
            _logger.LogError("Error getting Source : {Message}", SourceResponse.Message);
            return false;
        }

        var sourceid = SourceResponse?.Payload?.Id;
        if (sourceid == null || string.IsNullOrEmpty(sourceid.ToString()))
        {
            _logger.LogError("Error getting Source payload : {Message}", SourceResponse.Message);
            return false;
        }

        var processedCount = 0;
        foreach (var product in allliveproducts)
        {
            
            processedCount++;
            if (processedCount % 50 == 0)
            {
                _logger.LogInformation("Processed {Count}/{Total}", processedCount, allliveproducts.Count);
            }
            var ldata = product.Value;
            var exists = await _productMapRepository.Get(ldata.Id);
            if (exists == null || exists.Id == Guid.Empty)
            {
                //new
                // we have sdata create an array of 200 items for sync data
                var condition = CMS_Scrappers.Services.Implementations.RRSyncService.MapConditionToRating(
                    ldata.ConditionGrade);
                var pdata = new CreateRRSyncProductRequest
                {
                    ProductName = ldata.Title,
                    Brand = ldata.Brand,
                    Sku = ldata.Sku ?? "",
                    Note = ldata.Description,
                    StockXId = ldata.Id.ToString(),
                    Category = ldata.Category,
                    Subcategory = "", //need to investigate the subcategory part
                    Gender = ldata.Gender,
                    ProductType = ldata.ProductType,
                    UrlKey = ldata.ProductUrl,
                    ThumbnailImage = ldata.Image.OrderBy(i => i.Priority).FirstOrDefault()?.Url ?? "",
                    Rating = condition,
                    ColorWay = "",
                    Images = ldata.Image
                        .OrderBy(i => i.Priority)
                        .Select((img, idx) => new CreateRRSyncProductImage
                        {
                            ImageUrl = img.Url,
                            Position = img.Priority ?? (idx + 1),
                            IsRemoveBackground = false,
                            IsThumbnail = false,
                        })
                        .ToList()
                };
                NewData.Add(pdata);

                if (NewData.Count == RatelimitCount)
                {
                    try
                    {
                        await FlushNewBatchAsync(NewData, sourceid, allliveproducts);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Batch flush threw — continuing");
                    }
                    finally
                    {
                        NewData.Clear();
                    }
                }
            }
            else
            {
                try
                {
                    await SyncExistingProductAsync(ldata);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating products ");
                }
               
            }
        }

        if (NewData.Any())
        {
            try
            {
                await FlushNewBatchAsync(NewData, sourceid, allliveproducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch flush threw — continuing");
            }
            finally
            {
                NewData.Clear();
            }
        }

        return true;
    }

    #region New Product Flow

    private async Task FlushNewBatchAsync(
        List<CreateRRSyncProductRequest> batch,
        string sourceId,
        Dictionary<string, Sdata> allLiveProducts)
    {
        if (!batch.Any()) return;

        var productResponse = await _syncService.PushNewProductBatchAsync(
            new BulkCreateRRSyncProductRequest
            {
                SourceId = sourceId,
                Products = batch
            });

        if (!productResponse.IsSuccess || productResponse.Payload == null)
        {
            _logger.LogError("Bulk product create failed: {Message}", productResponse.Message);
            return;
        }

        foreach (var (cmsProductId, syncProductId) in productResponse.Payload)
        {
            if (!allLiveProducts.TryGetValue(cmsProductId, out var cmsProduct))
            {
                _logger.LogError("Unknown product ID returned from RRSync: {Id}", cmsProductId);
                continue;
            }

            await PushVariantsForNewProductAsync(cmsProductId, syncProductId, cmsProduct);
        }
    }

    private async Task PushVariantsForNewProductAsync(
        string cmsProductId,
        string syncProductId,
        Sdata cmsProduct)
    {
        var variantRequests = cmsProduct.Variants
            .Select(PrepareRRSyncvarinat)
            .ToList();

        if (!variantRequests.Any())
        {
            _logger.LogWarning("Product {Id} has no variants to push", cmsProductId);
            return;
        }

        var variantResponse = await _syncService.PushNewVariantBatchAsync(syncProductId, variantRequests);

        if (!variantResponse.IsSuccess || variantResponse.Payload == null)
        {
            _logger.LogError("Variant push failed for product {Id}: {Message}",
                syncProductId, variantResponse.Message);
            return;
        }

        await StoreMapping(cmsProductId, syncProductId, variantResponse.Payload);
    }

    #endregion
    
    #region Helper Map Table functions

    //variantMap cmsvariantid, syncvariantid;
    private async Task StoreMapping(string cmsProductId,
        string syncProductId,
        Dictionary<string, string> variantMap)
    {
        if (string.IsNullOrWhiteSpace(cmsProductId)
            || string.IsNullOrWhiteSpace(syncProductId)
            || variantMap == null)
        {
            _logger.LogWarning("StoreMapping called with missing args");
            return;
        }

        if (!Guid.TryParse(cmsProductId, out var sdataId))
        {
            _logger.LogWarning("Invalid CMS product id: {CmsProductId}", cmsProductId);
            return;
        }

        var productMap = new RRSyncProductMap
        {
            SdataId = sdataId,
            RRSyncProductId = syncProductId,
            SyncStatus = "Active"
        };
        var productMapId = await _productMapRepository.Insertmap(productMap);

        if (productMapId == Guid.Empty)
        {
            _logger.LogError("Failed to insert product map for {SdataId}", sdataId);
            return;
        }

        foreach (var (cmsVariantIdStr, syncVariantId) in variantMap)
        {
            if (!long.TryParse(cmsVariantIdStr, out var cmsVariantId))
            {
                _logger.LogWarning("Invalid CMS variant id: {Id}", cmsVariantIdStr);
                continue;
            }

            await _variantMapRepository.insert(new RRSyncVariantMap
            {
                VariantId = cmsVariantId,
                RRSyncProductMapId = productMapId,
                RRSyncVariantId = syncVariantId,
                SyncStatus = "Active"
            });
        }
    }

    private CreateRRSyncVariantRequest PrepareRRSyncvarinat(ProductVariantRecord variant)
    {
        var vareobj = new CreateRRSyncVariantRequest
        {
            Size = variant.Size,
            Color = "",
            Qty = variant.InStock ? 1 : 0,
            SellPrice = _syncService.Addmarkup(variant.Price),
            CostPrice = _syncService.ToGbp(variant.Price),
            Fees = 0,
            Profit = 0,
            sourcevariantid = variant.Id.ToString()
        };
        return vareobj;
    }

    #endregion
    
    #region Update Flow

    private async Task SyncExistingProductAsync(Sdata ldata)
    {
        var productMap = await _productMapRepository.Get(ldata.Id);
        if (productMap == null || productMap.Id == Guid.Empty)
        {
            _logger.LogWarning("Expected map for {SdataId} but none found", ldata.Id);
            return;
        }

        var rrProductId = productMap.RRSyncProductId;

        var rrVariantsResponse = await _syncService.GetAllSyncVarinats(rrProductId);
        if (!rrVariantsResponse.IsSuccess || rrVariantsResponse.Payload == null)
        {
            _logger.LogError("Failed to fetch RRSync variants for {Id}: {Msg}",
                rrProductId, rrVariantsResponse.Message);
            return;
        }

        var rrVariants = rrVariantsResponse.Payload;
        var mapsByRRSyncId = await _variantMapRepository.GetAll(productMap.Id);
        var cmsByVariantId = ldata.Variants.ToDictionary(v => v.Id);
        var cmsVariantIdsWithMaps = mapsByRRSyncId.Values
            .Select(m => m.VariantId)
            .ToHashSet();

        await UpdateChangedVariantsAsync(rrProductId, rrVariants, mapsByRRSyncId, cmsByVariantId);
        await CreateMissingVariantsAsync(rrProductId, ldata.Variants, cmsVariantIdsWithMaps, productMap.Id);
    }

    private async Task UpdateChangedVariantsAsync(
        string rrProductId,
        List<RRSyncVarinatDTO> rrVariants,
        Dictionary<string, RRSyncVariantMap> mapsByRRSyncId,
        Dictionary<long, ProductVariantRecord> cmsByVariantId)
    {
        var updates = new List<UpdateRRSyncVariantRequest>();

        foreach (var rrVariant in rrVariants)
        {
            if (!mapsByRRSyncId.TryGetValue(rrVariant.Id, out var vm))
            {
                // Orphan on RRSync side — delete
                _logger.LogWarning("RRSync variant {Id} has no CMS map — deleting", rrVariant.Id);
                var delresonse = await _syncService.DeleteVariantAsync(rrVariant.Id);
                if (!delresonse.IsSuccess || delresonse.Payload == null)
                {
                    _logger.LogError("Error upon variant deletion: {Message}", delresonse.Message);
                  
                }
                continue;
            }

            if (!cmsByVariantId.TryGetValue(vm.VariantId, out var cmsVariant))
            {
                _logger.LogWarning("CMS variant {Id} gone but map exists", vm.VariantId);
                // TODO: decide whether to delete on RRSync + mark map stale
                
                await _variantMapRepository.UpdateStatus(vm.VariantId,"OrphanCandidate");
                continue;
            }

            var update = TryBuildVariantUpdate(rrVariant, cmsVariant);
            if (update != null) updates.Add(update);
        }

        if (!updates.Any()) return;

        var response = await _syncService.UpdateVariantBatchAsync(updates, rrProductId);
        if (!response.IsSuccess)
        {
            _logger.LogError("Update batch failed for product {Id}: {Msg}",
                rrProductId, response.Message);
        }
        var rrsyncVariantIds = updates.Select(u => u.Id).ToList();
        _variantMapRepository.TouchVariantMapsAfterSync(rrsyncVariantIds);
    }

    private UpdateRRSyncVariantRequest? TryBuildVariantUpdate(
        RRSyncVarinatDTO rrVariant,
        ProductVariantRecord cmsVariant)
    {
        var costChanged = _syncService.ToGbp(cmsVariant.Price) != rrVariant.CostPrice;
        var sellChanged = _syncService.Addmarkup(cmsVariant.Price) != rrVariant.SellPrice;
        var stockChanged = (cmsVariant.InStock && rrVariant.Quantity < 1)
                           || (!cmsVariant.InStock && rrVariant.Quantity > 0);

        if (!costChanged && !sellChanged && !stockChanged) return null;

        return new UpdateRRSyncVariantRequest
        {
            Id = rrVariant.Id,
            Size = rrVariant.Size,
            Color = rrVariant.Color,
            Qty = stockChanged ? (cmsVariant.InStock ? 1 : 0) : (rrVariant.Quantity ?? 0),
            SellPrice = sellChanged ? _syncService.Addmarkup(cmsVariant.Price) : rrVariant.SellPrice,
            CostPrice = costChanged ? _syncService.ToGbp(cmsVariant.Price) : rrVariant.CostPrice,
            Fees = rrVariant.Fees,
            Profit = rrVariant.Profit
        };
    }

    private async Task CreateMissingVariantsAsync(
        string rrProductId,
        List<ProductVariantRecord> cmsVariants,
        HashSet<long> cmsVariantIdsWithMaps,
        Guid productMapId)
    {
        var newCmsVariants = cmsVariants
            .Where(v => !cmsVariantIdsWithMaps.Contains(v.Id))
            .ToList();

        if (!newCmsVariants.Any()) return;

        var requests = newCmsVariants.Select(PrepareRRSyncvarinat).ToList();

        foreach (var v in newCmsVariants)
            _logger.LogInformation("New variant to create on RRSync: {Id}", v.Id);

        var response = await _syncService.PushNewVariantBatchAsync(rrProductId, requests);
        if (!response.IsSuccess || response.Payload == null)
        {
            _logger.LogError("Failed to create new variants for product {Id}: {Msg}",
                rrProductId, response.Message);
            return;
        }

        // Persist the new variant maps so next run sees them as known
        foreach (var (cmsVariantIdStr, syncVariantId) in response.Payload)
        {
            if (!long.TryParse(cmsVariantIdStr, out var cmsVariantId))
            {
                _logger.LogWarning("Invalid CMS variant id from RRSync response: {Id}", cmsVariantIdStr);
                continue;
            }

            await _variantMapRepository.insert(new RRSyncVariantMap
            {
                VariantId = cmsVariantId,
                RRSyncProductMapId = productMapId,
                RRSyncVariantId = syncVariantId,
                SyncStatus = "Active"
            });
        }
    }

    #endregion
}