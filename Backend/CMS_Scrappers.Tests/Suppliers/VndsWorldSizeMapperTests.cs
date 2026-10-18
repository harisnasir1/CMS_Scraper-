using CMS_Scrappers.Suppliers.VNDSWORLD;

namespace CMS_Scrappers.Tests.Suppliers;

public class VndsWorldSizeMapperTests
{
    private readonly VndsWorldSizeMapper _mapper = new();

    [Theory]
    [InlineData("Small", "S")]
    [InlineData("Medium", "M")]
    [InlineData("Large", "L")]
    [InlineData("XL", "XL")]
    [InlineData("XXL", "XXL")]
    [InlineData("XS", "XS")]
    [InlineData("S/M", "S")]
    [InlineData("M/L", "M")]
    [InlineData("IT48", "M")]
    [InlineData("IT56", "XXXL")]
    public void Clothing_letter_sizes(string raw, string expected)
    {
        Assert.Equal(expected, _mapper.GetMappedSizes("Clothing", "T-Shirts & Tops", raw, "Men"));
    }

    [Theory]
    [InlineData("W30", "W 30")]
    [InlineData("W32", "W 32")]
    [InlineData("W34", "W 34")]
    [InlineData("W36", "W 36")]
    [InlineData("W38", "W 38")]
    [InlineData("W 31", "W 31")]
    public void Waist_sizes(string raw, string expected)
    {
        Assert.Equal(expected, _mapper.GetMappedSizes("Clothing", "Jeans & Bottoms", raw, "Men"));
    }

    [Theory]
    [InlineData("UK7", "UK 7")]
    [InlineData("UK9.5", "UK 9.5")]
    [InlineData("UK10", "UK 10")]
    [InlineData("UK10.5", "UK 10.5")]
    [InlineData("UK11", "UK 11")]
    [InlineData("UK6", "UK 6 (EU 40)")]
    [InlineData("EU42", "UK 8")]
    [InlineData("EU43", "UK 9")]
    [InlineData("EU43.5", "UK 9.5")]
    [InlineData("EU44", "UK 9.5")]
    [InlineData("EU44.5", "UK 10")]
    [InlineData("EU45", "UK 10.5")]
    [InlineData("EU38 (w)", "UK 5")]
    public void Footwear_sizes(string raw, string expected)
    {
        Assert.Equal(expected, _mapper.GetMappedSizes("Footwear", "Trainers", raw, "Men"));
    }

    [Theory]
    [InlineData("OS")]
    [InlineData("7 3/8")]
    [InlineData("7 1/2")]
    [InlineData("45cm")]
    [InlineData("11inch")]
    [InlineData("8x5inch")]
    [InlineData("MM")]
    public void One_size_values_become_OS(string raw)
    {
        Assert.Equal("O/S", _mapper.GetMappedSizes("Clothing", "T-Shirts & Tops", raw, "Men"));
    }

    [Fact]
    public void Accessories_always_collapse_to_OS()
    {
        Assert.Equal("O/S", _mapper.GetMappedSizes("Accessories", "Hats", "Medium", "Men"));
        Assert.Equal("O/S", _mapper.GetMappedSizes("Accessories", "Bags", "anything", "Men"));
    }

    [Theory]
    [InlineData("Medium DXB")]
    [InlineData("OS DXB")]
    [InlineData("W34 DXB")]
    [InlineData("EU45 DXB")]
    public void Dubai_variants_are_skipped(string raw)
    {
        Assert.Null(_mapper.GetMappedSizes("Clothing", "T-Shirts & Tops", raw, "Men"));
        Assert.Null(_mapper.GetMappedSizes("Accessories", "Hats", raw, "Men"));
    }

    [Theory]
    [InlineData("Clothing", "")]
    [InlineData("Clothing", "Purple")]
    [InlineData("Footwear", "Medium")]
    [InlineData("Footwear", "EU99")]
    [InlineData("", "Medium")]
    public void Unsupported_values_are_skipped(string category, string raw)
    {
        Assert.Null(_mapper.GetMappedSizes(category, "x", raw, "Men"));
    }
}
