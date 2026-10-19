using CMS_Scrappers.Data.Enums;
using CMS_Scrappers.Pricing;
using CMS_Scrappers.Pricing.Policies;

namespace CMS_Scrappers.Tests.Pricing;

public class PricingPolicyResolverTests
{
    private readonly PricingPolicyResolver _resolver = new();

    [Fact]
    public void Savonches_to_RRSync_is_plain_conversion()
    {
        Assert.IsType<UsdToGbpPolicy>(_resolver.For("Savonches", PricingTarget.RRSync));
    }

    [Fact]
    public void Savonches_to_Shopify_keeps_the_markup()
    {
        Assert.IsType<UsdMarkupToGbpPolicy>(_resolver.For("Savonches", PricingTarget.Shopify));
    }

    [Theory]
    [InlineData("savonches")]
    [InlineData("SAVONCHES")]
    [InlineData("  Savonches  ")]
    public void Name_match_ignores_case_and_whitespace(string name)
    {
        Assert.NotNull(_resolver.For(name, PricingTarget.RRSync));
    }

    [Fact]
    public void Unknown_supplier_throws_instead_of_defaulting()
    {
        Assert.Throws<InvalidOperationException>(() => _resolver.For("someone-else", PricingTarget.RRSync));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_throws(string name)
    {
        Assert.Throws<ArgumentException>(() => _resolver.For(name, PricingTarget.RRSync));
    }
}
