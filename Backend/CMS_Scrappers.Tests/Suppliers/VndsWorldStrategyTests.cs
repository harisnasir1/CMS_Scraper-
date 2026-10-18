using CMS_Scrappers.Suppliers.VNDSWORLD;

namespace CMS_Scrappers.Tests.Suppliers;

public class VndsWorldStrategyTests
{
    [Fact]
    public void StripHtml_removes_tags_decodes_entities_and_keeps_paragraph_breaks()
    {
        var html = "<p>Supreme &amp; Chrome Hearts</p><p>Size   Medium<br>Condition: <strong>New</strong></p>";
        var text = VndsWorldStrategy.StripHtml(html);
        Assert.Equal("Supreme & Chrome Hearts\nSize Medium\nCondition: New", text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripHtml_handles_empty(string? html)
    {
        Assert.Equal("", VndsWorldStrategy.StripHtml(html));
    }
}
