using CMS_Scrappers.Data.Enums;
using CMS_Scrappers.Pricing;
using CMS_Scrappers.Pricing.Policies;

namespace CMS_Scrappers.Tests.Pricing;

public class PassthroughPolicyTests
{
    private readonly PassthroughPolicy _policy = new();

    [Theory]
    [InlineData(100)]
    [InlineData(1313.82)]
    [InlineData(0.01)]
    public void Sell_and_cost_are_the_supplier_price_unchanged(decimal gbp)
    {
        Assert.Equal(gbp, _policy.Sell(gbp));
        Assert.Equal(gbp, _policy.Cost(gbp));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Non_positive_price_gives_zero(decimal gbp)
    {
        Assert.Equal(0, _policy.Sell(gbp));
        Assert.Equal(0, _policy.Cost(gbp));
    }

    [Fact]
    public void VNDSWORLD_is_passthrough_for_both_destinations()
    {
        var resolver = new PricingPolicyResolver();
        Assert.IsType<PassthroughPolicy>(resolver.For("VNDSWORLD", PricingTarget.RRSync));
        Assert.IsType<PassthroughPolicy>(resolver.For("vndsworld", PricingTarget.Shopify));
    }
}
