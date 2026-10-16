using CMS_Scrappers.Pricing.Policies;
using CMS_Scrappers.Pricing.Rules;

namespace CMS_Scrappers.Tests.Pricing;

/// <summary>
/// RRSync pricing for a USD supplier: plain conversion, no markup, sell equals cost.
/// </summary>
public class UsdToGbpPolicyTests
{
    private static readonly UsdToGbpPolicy Policy = new(SavonchesRules.UsdToGbp);

    [Theory]
    [InlineData(100, 80.00)]
    [InlineData(1234, 987.20)]
    [InlineData(0.15625, 0.12)]  // banker's rounding, same as cost
    public void Sell_is_base_times_rate_to_the_penny(decimal usd, decimal expectedGbp)
    {
        Assert.Equal(expectedGbp, Policy.Sell(usd));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1234)]
    [InlineData(0.15625)]
    [InlineData(55.55)]
    public void Sell_and_cost_are_identical(decimal usd)
    {
        Assert.Equal(Policy.Cost(usd), Policy.Sell(usd));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Non_positive_price_gives_zero(decimal usd)
    {
        Assert.Equal(0, Policy.Sell(usd));
        Assert.Equal(0, Policy.Cost(usd));
    }

    [Fact]
    public void Rejects_non_positive_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdToGbpPolicy(0));
    }
}
