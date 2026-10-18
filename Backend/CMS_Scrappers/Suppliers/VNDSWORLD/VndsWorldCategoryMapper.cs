namespace CMS_Scrappers.Suppliers.VNDSWORLD;

/// <summary>
/// VNDSWORLD product_type -> our canonical (Category, ProductType).
/// Right-hand values are RRSync's seeded names (ResellerSync ShopifyTaxonomySeeder); RRSync creates
/// any name it does not know, so only use names from that list.
/// Built from the catalog-summary dump of 2026-10-18 (552 products, 22 product types).
/// </summary>
public class VndsWorldCategoryMapper : ICategoryMappingStrategy
{
    private static readonly Dictionary<string, (string Category, string ProductType)> _map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Clothing
            { "T-Shirt",    ("Clothing", "T-Shirts & Tops") },
            { "Jersey",     ("Clothing", "T-Shirts & Tops") },
            { "Hoodie",     ("Clothing", "Hoodies & Sweatshirts") },
            { "Sweatshirt", ("Clothing", "Hoodies & Sweatshirts") },
            { "Shorts",     ("Clothing", "Shorts") },
            { "Jacket",     ("Clothing", "Coats & Jackets") },
            { "Joggers",    ("Clothing", "Jeans & Bottoms") },
            { "Jeans",      ("Clothing", "Jeans & Bottoms") },
            { "Trousers",   ("Clothing", "Jeans & Bottoms") },
            { "Knitwear",   ("Clothing", "Knitwear") },
            { "Shirt",      ("Clothing", "Shirts & Polo Shirts") },
            { "Tracksuit",  ("Clothing", "Activewear") },

            // Footwear (all 24 tagged Sneakers in the dump)
            { "Shoes",      ("Footwear", "Trainers") },
            { "Footwear",   ("Footwear", "Trainers") },

            // Accessories
            { "Hat",        ("Accessories", "Hats") },
            { "Headwear",   ("Accessories", "Hats") },
            { "Bag",        ("Accessories", "Bags") },
            { "Wallet",     ("Accessories", "Wallets") },
            { "Balaclava",  ("Accessories", "Winter Accessories") },
            { "Sunglasses", ("Accessories", "Sunglasses") },
            { "Belt",       ("Accessories", "Belts") },

            // "Accessory" (16 products) is deliberately unmapped until its titles are reviewed;
            // unmapped products stay Categorized with empty category and never auto-approve.
        };

    public (string TrendsCategory, string TrendsProductType) GetCategory(string productType)
    {
        if (string.IsNullOrWhiteSpace(productType)) return (string.Empty, string.Empty);
        return _map.TryGetValue(productType.Trim(), out var hit) ? hit : (string.Empty, string.Empty);
    }
}
