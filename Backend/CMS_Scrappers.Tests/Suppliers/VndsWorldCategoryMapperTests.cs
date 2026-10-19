using CMS_Scrappers.Suppliers.VNDSWORLD;

namespace CMS_Scrappers.Tests.Suppliers;

public class VndsWorldCategoryMapperTests
{
    private readonly VndsWorldCategoryMapper _mapper = new();

    [Theory]
    [InlineData("Hat", "Accessories", "Hats")]
    [InlineData("Headwear", "Accessories", "Hats")]
    [InlineData("T-Shirt", "Clothing", "T-Shirts & Tops")]
    [InlineData("Jersey", "Clothing", "T-Shirts & Tops")]
    [InlineData("Hoodie", "Clothing", "Hoodies & Sweatshirts")]
    [InlineData("Sweatshirt", "Clothing", "Hoodies & Sweatshirts")]
    [InlineData("Shorts", "Clothing", "Shorts")]
    [InlineData("Jacket", "Clothing", "Coats & Jackets")]
    [InlineData("Joggers", "Clothing", "Jeans & Bottoms")]
    [InlineData("Jeans", "Clothing", "Jeans & Bottoms")]
    [InlineData("Trousers", "Clothing", "Jeans & Bottoms")]
    [InlineData("Knitwear", "Clothing", "Knitwear")]
    [InlineData("Shirt", "Clothing", "Shirts & Polo Shirts")]
    [InlineData("Tracksuit", "Clothing", "Activewear")]
    [InlineData("Shoes", "Footwear", "Trainers")]
    [InlineData("Footwear", "Footwear", "Trainers")]
    [InlineData("Bag", "Accessories", "Bags")]
    [InlineData("Wallet", "Accessories", "Wallets")]
    [InlineData("Balaclava", "Accessories", "Winter Accessories")]
    [InlineData("Sunglasses", "Accessories", "Sunglasses")]
    [InlineData("Belt", "Accessories", "Belts")]
    public void Every_product_type_seen_in_the_dump_maps(string raw, string category, string productType)
    {
        Assert.Equal((category, productType), _mapper.GetCategory(raw));
    }

    [Theory]
    [InlineData("hat")]
    [InlineData(" T-SHIRT ")]
    public void Match_ignores_case_and_whitespace(string raw)
    {
        Assert.NotEqual(string.Empty, _mapper.GetCategory(raw).TrendsCategory);
    }

    [Theory]
    [InlineData("Accessory")]   // deliberately unmapped until titles are reviewed
    [InlineData("")]
    [InlineData("Something new")]
    public void Unknown_type_is_left_empty_so_it_never_auto_approves(string raw)
    {
        Assert.Equal((string.Empty, string.Empty), _mapper.GetCategory(raw));
    }
}
