namespace CMS_Scrappers.Pricing.Policies;

/// <summary>
/// Straight currency conversion, no markup. Sell and cost are the same number.
/// Used when the destination prices the item itself and only needs our cost in its currency.
/// </summary>
public sealed class UsdToGbpPolicy : IPricingPolicy
{
    private readonly decimal _rate;

    public UsdToGbpPolicy(decimal usdToGbpRate)
    {
        if (usdToGbpRate <= 0) throw new ArgumentOutOfRangeException(nameof(usdToGbpRate));
        _rate = usdToGbpRate;
    }

    public decimal Sell(decimal basePrice) => Convert(basePrice);

    public decimal Cost(decimal basePrice) => Convert(basePrice);

    private decimal Convert(decimal usd)
    {
        if (usd <= 0) return 0;
        return Math.Round(usd * _rate, 2);
    }
}
