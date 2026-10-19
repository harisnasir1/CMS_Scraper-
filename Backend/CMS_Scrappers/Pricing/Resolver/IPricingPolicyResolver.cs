using CMS_Scrappers.Data.Enums;
using CMS_Scrappers.Pricing.Policies;

namespace CMS_Scrappers.Pricing;

public interface IPricingPolicyResolver
{
    /// <summary>
    /// The policy for one supplier and one destination. Throws when no policy is registered,
    /// so a missing rule stops the sync instead of pushing a default price.
    /// </summary>
    IPricingPolicy For(string scraperName, PricingTarget target);
}
