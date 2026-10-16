using CMS_Scrappers.Pricing.Policies;
using CMS_Scrappers.Pricing.Rules;

namespace CMS_Scrappers.Tests.Pricing;

/// <summary>
/// Pins the Shopify-store pricing to the numbers the old ShopifyService.Addmarkup / RRSyncService.ToGbp
/// produced. If any of these fail, Shopify prices would move on the next sync.
/// </summary>
public class UsdMarkupToGbpPolicyTests
{
    private static readonly UsdMarkupToGbpPolicy Policy =
        new(SavonchesRules.UsdToGbp, SavonchesRules.ShopifyMarkup, SavonchesRules.ShopifyRoundTo);

    [Theory]
    [InlineData(100, 95)]        // 100 * 1.2 * 0.8 = 96   -> nearest 5 = 95
    [InlineData(1234, 1185)]     // 1234 * 1.2 * 0.8 = 1184.64 -> 1185
    [InlineData(500, 480)]       // 500 * 1.2 * 0.8 = 480  -> exact
    [InlineData(2.60, 0)]        // 2.60 * 0.96 = 2.496 -> 0.499 -> rounds to 0
    [InlineData(2.61, 5)]        // 2.61 * 0.96 = 2.5056 -> 0.501 -> rounds to 5
    public void Sell_matches_old_Addmarkup(decimal usd, decimal expectedGbp)
    {
        Assert.Equal(expectedGbp, Policy.Sell(usd));
    }

    [Theory]
    [InlineData(100, 80.00)]
    [InlineData(1234, 987.20)]
    [InlineData(0.15625, 0.12)]  // 0.125 -> banker's rounding gives 0.12, not 0.13
    [InlineData(0.21875, 0.18)]  // 0.175 -> 0.18 (even)
    public void Cost_matches_old_ToGbp(decimal usd, decimal expectedGbp)
    {
        Assert.Equal(expectedGbp, Policy.Cost(usd));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_price_gives_zero(decimal usd)
    {
        Assert.Equal(0, Policy.Sell(usd));
        Assert.Equal(0, Policy.Cost(usd));
    }

    [Fact]
    public void Sell_is_always_a_multiple_of_the_shelf_rounding()
    {
        foreach (var usd in new[] { 1m, 7m, 99.99m, 123.45m, 999m, 4321m })
        {
            Assert.Equal(0, Policy.Sell(usd) % SavonchesRules.ShopifyRoundTo);
        }
    }
}
