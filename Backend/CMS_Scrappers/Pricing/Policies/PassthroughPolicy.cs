namespace CMS_Scrappers.Pricing.Policies;

/// <summary>
/// No conversion, no markup: the supplier already prices in the destination's currency and we list
/// at their price. Sell and cost are the same number. Reusable for any supplier in that position.
/// </summary>
public sealed class PassthroughPolicy : IPricingPolicy
{
    public decimal Sell(decimal basePrice) => basePrice <= 0 ? 0 : basePrice;

    public decimal Cost(decimal basePrice) => basePrice <= 0 ? 0 : basePrice;
}
