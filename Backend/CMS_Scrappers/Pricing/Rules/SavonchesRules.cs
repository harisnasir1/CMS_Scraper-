namespace CMS_Scrappers.Pricing.Rules;

/// <summary>
/// The only place the Savonches pricing numbers are allowed to live.
/// Phase 1: hardcoded. Phase 2 moves the rate behind the supplier's stored currency and config.
/// </summary>
public static class SavonchesRules
{
   
    public const decimal UsdToGbp = 0.8m;

    /// <summary>Markup applied on top of the GBP price when the destination is our Shopify stores.</summary>
    public const decimal ShopifyMarkup = 0.20m;

    /// <summary>Shopify sell prices are rounded to the nearest multiple of this, in GBP.</summary>
    public const decimal ShopifyRoundTo = 5m;
}
