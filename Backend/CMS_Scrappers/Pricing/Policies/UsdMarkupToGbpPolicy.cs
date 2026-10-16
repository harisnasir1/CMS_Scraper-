namespace CMS_Scrappers.Pricing.Policies;

/// <summary>
/// Currency conversion plus a markup on the sell side, with the sell price rounded to a
/// "shelf" multiple (e.g. nearest £5). Cost is the plain converted price.
/// This is exactly the formula the Shopify push used before the pricing layer existed;
/// the tests pin its outputs so Shopify prices do not move.
/// </summary>
public sealed class UsdMarkupToGbpPolicy : IPricingPolicy
{
    private readonly decimal _rate;
    private readonly decimal _markup;
    private readonly decimal _roundTo;

    public UsdMarkupToGbpPolicy(decimal usdToGbpRate, decimal markup, decimal roundSellTo)
    {
        if (usdToGbpRate <= 0) throw new ArgumentOutOfRangeException(nameof(usdToGbpRate));
        if (markup < 0) throw new ArgumentOutOfRangeException(nameof(markup));
        if (roundSellTo <= 0) throw new ArgumentOutOfRangeException(nameof(roundSellTo));
        _rate = usdToGbpRate;
        _markup = markup;
        _roundTo = roundSellTo;
    }

    public decimal Sell(decimal basePrice)
    {
        if (basePrice <= 0) return 0;
        // Order matters and is kept as it was: markup first, then convert, then round to the shelf multiple
        decimal withMarkup = basePrice * (1 + _markup);
        decimal inPounds = withMarkup * _rate;
        return Math.Round(inPounds / _roundTo, MidpointRounding.AwayFromZero) * _roundTo;
    }

    public decimal Cost(decimal basePrice)
    {
        if (basePrice <= 0) return 0;
        // Default (banker's) rounding on purpose: this must equal the old ToGbp() to the penny,
        // otherwise every midpoint cost would look "changed" on the next sync.
        return Math.Round(basePrice * _rate, 2);
    }
}
