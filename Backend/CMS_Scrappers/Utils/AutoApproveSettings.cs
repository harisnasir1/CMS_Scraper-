namespace CMS_Scrappers.Utils;

/// <summary>
/// Settings for the daily job that moves "Categorized" products to "Live"
/// without a manual review. Bound from the "AutoApprove" section of appsettings.
/// </summary>
public class AutoApproveSettings
{
    /// <summary>
    /// Off by default so a deploy never starts pushing products Live on its own.
    /// Turn on in production with the env var AutoApprove__Enabled=true.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Hour of the day (UTC, 0-23) when the job runs.</summary>
    public int RunAtUtcHour { get; set; } = 3;

    /// <summary>Name of the scraper row whose products are approved (matches Scrappers.Name).</summary>
    public string ScraperName { get; set; } = "Savonches";

    
    public const int HardMaxPerRun = 100;

   public int? MaxProductsPerRun { get; set; } = HardMaxPerRun;

    public int EffectiveLimit(int? requested = null)
    {
        var limit = HardMaxPerRun;
        if (MaxProductsPerRun is > 0 and < HardMaxPerRun) limit = MaxProductsPerRun.Value;
        if (requested is > 0 && requested.Value < limit) limit = requested.Value;
        return limit;
    }

    /// <summary>Products with fewer images than this stay in review.</summary>
    public int MinImages { get; set; } = 1;

    /// <summary>A product needs at least one in-stock variant priced at or above this.</summary>
    public decimal MinVariantPrice { get; set; } = 0;

    public List<string> ExcludedBrands { get; set; } = new() { "Goyard" };

    /// <summary>
    /// Image rules by image count. The tier with the largest MinTotal that is
    /// less than or equal to the product's image count applies.
    /// </summary>
    public List<ImageTier> ImageTiers { get; set; } = new()
    {
        // All images are kept (DropLast = 0); the tiers only decide how many get background removal
        new ImageTier { MinTotal = 1, DropLast = 0, BgRemove = 0 },
        new ImageTier { MinTotal = 2, DropLast = 0, BgRemove = 1 },
        new ImageTier { MinTotal = 4, DropLast = 0, BgRemove = 1 },
        new ImageTier { MinTotal = 6, DropLast = 0, BgRemove = 2 },
        new ImageTier { MinTotal = 7, DropLast = 0, BgRemove = 2 },
    };

    /// <summary>When true, every Shopify store is bulk-synced after a run that approved products.</summary>
    public bool SyncShopifyAfterApprove { get; set; } = false;

    public ImageTier TierFor(int totalImages)
    {
        return ImageTiers
            .Where(t => t.MinTotal <= totalImages)
            .OrderByDescending(t => t.MinTotal)
            .FirstOrDefault() ?? new ImageTier();
    }
}

public class ImageTier
{
    /// <summary>Tier applies when the product has at least this many images.</summary>
    public int MinTotal { get; set; } = 0;

    /// <summary>How many images to delete from the end of the ordered list (closeups).</summary>
    public int DropLast { get; set; } = 0;

    /// <summary>How many of the first remaining images get background removal.</summary>
    public int BgRemove { get; set; } = 0;
}
