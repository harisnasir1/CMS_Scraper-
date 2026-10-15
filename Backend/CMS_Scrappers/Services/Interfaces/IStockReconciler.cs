namespace CMS_Scrappers.Services.Interfaces;

public class StockReconcileResult
{
    public string Store { get; set; } = "";
    public bool DryRun { get; set; }
    /// <summary>Mapped variants we checked on Shopify.</summary>
    public int Checked { get; set; }
    /// <summary>Shopify already matched the supplier stock.</summary>
    public int Matched { get; set; }
    /// <summary>Variants whose Shopify quantity differs from the supplier (what a run changes, or would change in a dry run).</summary>
    public int ToZero { get; set; }
    public int ToOne { get; set; }
    public int Fixed { get; set; }
    public int Failed { get; set; }
    /// <summary>Inventory items we could not compare: NotOnShopify + NoLevelAtLocation.</summary>
    public int Missing { get; set; }
    /// <summary>Shopify does not know the inventory item at all (variant or product deleted).</summary>
    public int NotOnShopify { get; set; }
    /// <summary>The item exists but has no stock level at the location we sync (moved to / only stocked at another location).</summary>
    public int NoLevelAtLocation { get; set; }
    /// <summary>Mapping rows whose InStock flag was corrected to match Shopify.</summary>
    public int DbRowsCorrected { get; set; }
    /// <summary>Product mappings marked Removed because the product no longer exists on Shopify (would-be count in a dry run).</summary>
    public int RetiredProducts { get; set; }
    /// <summary>Variant mapping rows deleted because their Shopify variant no longer exists (would-be count in a dry run).</summary>
    public int RetiredVariants { get; set; }
    public bool Aborted { get; set; }
    public string? AbortReason { get; set; }
    public List<string> Errors { get; set; } = new();
    /// <summary>Up to 30 of the differences (to-zero first) for spot-checking a dry run.</summary>
    public List<StockReconcileSample> Samples { get; set; } = new();
}

public class StockReconcileSample
{
    public string Title { get; set; } = "";
    public string ProductUrl { get; set; } = "";
    public string Size { get; set; } = "";
    public string Sku { get; set; } = "";
    public bool SupplierInStock { get; set; }
    public int ShopifyQty { get; set; }
    public string ShopifyVariantId { get; set; } = "";
}

public interface IStockReconciler
{
    /// <summary>
    /// Reads the real "available" quantity of every mapped variant from Shopify, compares it with
    /// the supplier stock in ProductVariants.InStock and pushes the difference. Also corrects
    /// VariantStoreMapping.InStock so the DB reflects what Shopify actually holds.
    /// </summary>
    Task<StockReconcileResult> ReconcileStoreAsync(Shopify store, bool dryRun = false, bool force = false, CancellationToken ct = default);

    Task<List<StockReconcileResult>> ReconcileAllStoresAsync(bool dryRun = false, bool force = false, CancellationToken ct = default);
}
