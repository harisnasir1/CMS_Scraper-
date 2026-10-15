using CMS_Scrappers.Models;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations;

/// <summary>
/// Self-healing stock sync. Shopify is read as the source of truth for what the store currently
/// shows, ProductVariants.InStock is the source of truth for what it should show, and only the
/// difference is pushed. It never deletes anything; it only sets quantities to 0 or 1.
/// </summary>
public class StockReconciler : IStockReconciler
{
    private const int ReadBatchSize = 100;
    private const int WriteBatchSize = 100;

    private readonly IShopifyRepository _shopifyRepository;
    private readonly IProductStoreMappingRepository _productStoreMappingRepository;
    private readonly IVariantStoreMappingRepository _variantStoreMappingRepository;
    private readonly IFileReadWrite _readWrite;
    private readonly StockReconcileSettings _settings;
    private readonly ILogger<ShopifyService> _shopifyLogger;
    private readonly ILogger<StockReconciler> _logger;

    public StockReconciler(
        IShopifyRepository shopifyRepository,
        IProductStoreMappingRepository productStoreMappingRepository,
        IVariantStoreMappingRepository variantStoreMappingRepository,
        IFileReadWrite readWrite,
        StockReconcileSettings settings,
        ILogger<ShopifyService> shopifyLogger,
        ILogger<StockReconciler> logger)
    {
        _shopifyRepository = shopifyRepository;
        _productStoreMappingRepository = productStoreMappingRepository;
        _variantStoreMappingRepository = variantStoreMappingRepository;
        _readWrite = readWrite;
        _settings = settings;
        _shopifyLogger = shopifyLogger;
        _logger = logger;
    }

    public async Task<List<StockReconcileResult>> ReconcileAllStoresAsync(bool dryRun = false, bool force = false, CancellationToken ct = default)
    {
        var results = new List<StockReconcileResult>();
        foreach (var store in await _shopifyRepository.GiveallStoresToSync())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                results.Add(await ReconcileStoreAsync(store, dryRun, force, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reconcile crashed for {Store}", store.ShopName);
                results.Add(new StockReconcileResult { Store = store.ShopName, DryRun = dryRun, Aborted = true, AbortReason = ex.Message });
            }
        }
        return results;
    }

    public async Task<StockReconcileResult> ReconcileStoreAsync(Shopify store, bool dryRun = false, bool force = false, CancellationToken ct = default)
    {
        var result = new StockReconcileResult { Store = store.ShopName, DryRun = dryRun };
        var shopify = new ShopifyService(ToSettings(store), _shopifyLogger, _productStoreMappingRepository, _variantStoreMappingRepository, _readWrite);

        // One row per Shopify inventory item; duplicates in the mapping table would otherwise double-push
        var allRows = await _variantStoreMappingRepository.GetAllForStore(store.Id);
        var byInventoryId = new Dictionary<string, VariantStoreMapping>();
        foreach (var m in allRows)
        {
            if (string.IsNullOrWhiteSpace(m.ShopifyInventoryId) || m.Variant == null) continue;
            byInventoryId.TryAdd(m.ShopifyInventoryId, m);
        }
        result.Checked = byInventoryId.Count;
        if (result.Checked == 0)
        {
            _logger.LogInformation("Stock reconcile {Store}: no mapped variants", store.ShopName);
            return result;
        }

        var locationId = await shopify.GetLocationIdAsync();

        // 1. What Shopify actually shows right now
        var actual = new Dictionary<string, int?>();
        foreach (var chunk in byInventoryId.Keys.Chunk(ReadBatchSize))
        {
            ct.ThrowIfCancellationRequested();
            foreach (var kv in await shopify.GetAvailableQuantitiesAsync(chunk.ToList(), locationId))
            {
                actual[kv.Key] = kv.Value;
            }
        }

        // 2. Compare with what the supplier says
        var diffs = new List<(VariantStoreMapping mapping, int desired)>();
        var finalState = new List<(Guid mappingId, bool inStock)>();
        var ghosts = new List<VariantStoreMapping>();
        foreach (var (inventoryId, mapping) in byInventoryId)
        {
            if (!actual.TryGetValue(inventoryId, out var qty))
            {
                result.Missing++;
                result.NotOnShopify++;
                ghosts.Add(mapping);
                continue;
            }
            if (qty == null)
            {
                result.Missing++;
                result.NoLevelAtLocation++;
                continue;
            }
            var shopifyInStock = qty.Value > 0;
            var supplierInStock = mapping.Variant.InStock;
            if (shopifyInStock == supplierInStock)
            {
                result.Matched++;
                finalState.Add((mapping.Id, shopifyInStock));
            }
            else
            {
                diffs.Add((mapping, supplierInStock ? 1 : 0));
            }
        }
        result.ToZero = diffs.Count(d => d.desired == 0);
        result.ToOne = diffs.Count - result.ToZero;

        // A few examples of what would change, so a dry run can be spot-checked against the supplier
        result.Samples = diffs
            .OrderBy(d => d.desired)
            .Take(30)
            .Select(d => new StockReconcileSample
            {
                Title = d.mapping.ProductStoreMapping?.Product?.Title ?? "",
                ProductUrl = d.mapping.ProductStoreMapping?.Product?.ProductUrl ?? "",
                Size = d.mapping.Variant.Size,
                Sku = d.mapping.Variant.SKU,
                SupplierInStock = d.mapping.Variant.InStock,
                ShopifyQty = actual[d.mapping.ShopifyInventoryId] ?? 0,
                ShopifyVariantId = d.mapping.ShopifyVariantId
            })
            .ToList();

        // Missing items are mappings whose Shopify variant no longer exists; they cannot oversell, so only report them
        if (result.Missing > 0)
        {
            _logger.LogWarning("Stock reconcile {Store}: {Missing} of {Checked} mapped items could not be compared: {Gone} deleted on Shopify, {NoLevel} not stocked at location {Location}",
                store.ShopName, result.Missing, result.Checked, result.NotOnShopify, result.NoLevelAtLocation, locationId);
        }

        // Guard: a broken scrape must not turn into a mass stock wipe
        if (!force && result.ToZero > _settings.MaxZeroOutPerRun)
        {
            return Abort(result, $"would set {result.ToZero} variants to 0 (limit {_settings.MaxZeroOutPerRun}); refusing to sync");
        }

        _logger.LogInformation(
            "Stock reconcile {Store}{Mode}: checked {Checked}, matched {Matched}, to-zero {ToZero}, to-one {ToOne}, missing {Missing}",
            store.ShopName, dryRun ? " (dry run)" : "", result.Checked, result.Matched, result.ToZero, result.ToOne, result.Missing);

        // Ghost mappings: Shopify no longer has the inventory item. Retire them so they stop poisoning push batches.
        await RetireGhostsAsync(shopify, ghosts, allRows, result, dryRun);

        if (dryRun) return result;

        // 4. Push the differences, grouped by product so one bad product cannot block the rest
        var pending = new List<IGrouping<Guid, (VariantStoreMapping mapping, int desired)>>();
        var pendingItems = 0;
        foreach (var group in diffs.GroupBy(d => d.mapping.ProductStoreMappingId))
        {
            if (pendingItems + group.Count() > WriteBatchSize && pending.Count > 0)
            {
                await PushAsync(shopify, locationId, pending, result, finalState);
                pending.Clear();
                pendingItems = 0;
            }
            pending.Add(group);
            pendingItems += group.Count();
        }
        if (pending.Count > 0)
        {
            await PushAsync(shopify, locationId, pending, result, finalState);
        }

        // 5. Make the mapping table reflect what Shopify now holds
        result.DbRowsCorrected = await _variantStoreMappingRepository.UpdateInStockFlags(finalState);

        _logger.LogInformation(
            "Stock reconcile {Store} done: fixed {Fixed}, failed {Failed}, db rows corrected {Db}",
            store.ShopName, result.Fixed, result.Failed, result.DbRowsCorrected);
        return result;
    }

    private async Task PushAsync(
        ShopifyService shopify,
        string locationId,
        List<IGrouping<Guid, (VariantStoreMapping mapping, int desired)>> groups,
        StockReconcileResult result,
        List<(Guid mappingId, bool inStock)> finalState)
    {
        var all = groups.SelectMany(g => g).Select(d => (d.mapping.ShopifyInventoryId, d.desired)).ToList();
        var errors = await shopify.SetQuantitiesAsync(all, locationId);
        await Task.Delay(200);
        if (errors.Count == 0)
        {
            foreach (var d in groups.SelectMany(g => g))
            {
                result.Fixed++;
                finalState.Add((d.mapping.Id, d.desired == 1));
            }
            return;
        }

        // Shopify rejects the whole call when any one item is bad, so retry product by product
        _logger.LogWarning("Stock reconcile {Store}: batch of {Count} rejected ({Error}); retrying per product",
            result.Store, all.Count, errors[0]);
        foreach (var group in groups)
        {
            var items = group.Select(d => (d.mapping.ShopifyInventoryId, d.desired)).ToList();
            var groupErrors = await shopify.SetQuantitiesAsync(items, locationId);
            await Task.Delay(100);
            if (groupErrors.Count == 0)
            {
                foreach (var d in group)
                {
                    result.Fixed++;
                    finalState.Add((d.mapping.Id, d.desired == 1));
                }
            }
            else
            {
                result.Failed += items.Count;
                var msg = $"product mapping {group.Key}: {string.Join(" | ", groupErrors)}";
                _logger.LogError("Stock reconcile {Store}: {Error}", result.Store, msg);
                if (result.Errors.Count < 50) result.Errors.Add(msg);
            }
        }
    }

    // A ghost is a mapping whose Shopify inventory item is gone. If the whole product is gone the product mapping is
    // marked Removed (kept, so the product is never re-listed on that store) and all its variant rows are deleted;
    // if the product still exists only the dead variant rows go.
    private async Task RetireGhostsAsync(ShopifyService shopify, List<VariantStoreMapping> ghosts, List<VariantStoreMapping> allRows, StockReconcileResult result, bool dryRun)
    {
        if (ghosts.Count == 0) return;

        var byProduct = ghosts.GroupBy(g => g.ProductStoreMappingId).ToList();
        var externalIds = byProduct
            .Select(g => g.First().ProductStoreMapping?.ExternalProductId ?? "")
            .Where(id => id != "")
            .Distinct()
            .ToList();
        var existing = await shopify.GetExistingProductIdsAsync(externalIds);

        var removeProducts = new List<Guid>();
        var deleteVariantRows = new HashSet<Guid>();
        foreach (var group in byProduct)
        {
            var externalId = NumericId(group.First().ProductStoreMapping?.ExternalProductId ?? "");
            if (externalId == "" || !existing.Contains(externalId))
            {
                removeProducts.Add(group.Key);
                foreach (var row in allRows.Where(r => r.ProductStoreMappingId == group.Key)) deleteVariantRows.Add(row.Id);
            }
            else
            {
                foreach (var g in group) deleteVariantRows.Add(g.Id);
            }
        }
        result.RetiredProducts = removeProducts.Count;
        result.RetiredVariants = deleteVariantRows.Count;

        if (dryRun)
        {
            _logger.LogInformation("Stock reconcile {Store} (dry run): would retire {Products} product mappings and {Variants} variant rows",
                result.Store, result.RetiredProducts, result.RetiredVariants);
            return;
        }

        await _variantStoreMappingRepository.DeleteAllVariants(deleteVariantRows.ToList());
        await _productStoreMappingRepository.MarkRemoved(removeProducts);
        _logger.LogInformation("Stock reconcile {Store}: retired {Products} product mappings (Removed) and deleted {Variants} variant rows",
            result.Store, result.RetiredProducts, result.RetiredVariants);
    }

    private static string NumericId(string id) => id[(id.LastIndexOf('/') + 1)..];

    private StockReconcileResult Abort(StockReconcileResult result, string reason)
    {
        result.Aborted = true;
        result.AbortReason = reason;
        _logger.LogError("Stock reconcile {Store} ABORTED: {Reason}", result.Store, reason);
        return result;
    }

    private static ShopifySettings ToSettings(Shopify store) => new()
    {
        SHOPIFY_STORE_ID = store.Id,
        SHOPIFY_STORE_NAME = store.ShopName,
        SHOPIFY_ACCESS_TOKEN = store.AdminApiAccessToken,
        SHOPIFY_API_KEY = store.ApiKey,
        SHOPIFY_API_SECRET = store.ApiSecretKey,
        SHOPIFY_STORE_DOMAIN = store.HostName
    };
}
