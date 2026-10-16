using CMS_Scrappers.Data.Enums;
using CMS_Scrappers.Pricing.Policies;
using CMS_Scrappers.Pricing.Rules;

namespace CMS_Scrappers.Pricing;

/// <summary>
/// The one table that says which pricing rule applies to which (supplier, destination).
/// Registered as a singleton; policies are stateless so sharing them is safe.
/// Phase 1: the table is hardcoded here. Phase 2 derives it from the supplier's stored currency.
/// </summary>
public sealed class PricingPolicyResolver : IPricingPolicyResolver
{
    private readonly Dictionary<(string Scraper, PricingTarget Target), IPricingPolicy> _policies;

    public PricingPolicyResolver()
    {
        _policies = new Dictionary<(string, PricingTarget), IPricingPolicy>
        {
            [(Key("Savonches"), PricingTarget.RRSync)] =
                new UsdToGbpPolicy(SavonchesRules.UsdToGbp),

            [(Key("Savonches"), PricingTarget.Shopify)] =
                new UsdMarkupToGbpPolicy(SavonchesRules.UsdToGbp, SavonchesRules.ShopifyMarkup, SavonchesRules.ShopifyRoundTo),
        };
    }

    public IPricingPolicy For(string scraperName, PricingTarget target)
    {
        if (string.IsNullOrWhiteSpace(scraperName))
            throw new ArgumentException("Scraper name is required to resolve a pricing policy", nameof(scraperName));

        if (_policies.TryGetValue((Key(scraperName), target), out var policy))
            return policy;

        throw new InvalidOperationException($"No pricing policy registered for '{scraperName}' -> {target}");
    }

    // Same normalisation on registration and on lookup, so "Savonches", " savonches " and "SAVONCHES" all match
    private static string Key(string scraperName) => scraperName.Trim().ToLowerInvariant();
}
