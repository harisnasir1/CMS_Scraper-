namespace CMS_Scrappers.Data.DTO;

/// <summary>
/// What a supplier's public products.json looks like, aggregated. Used to build the
/// category and size mapping for a new supplier before it is enabled.
/// </summary>
public class CatalogSummary
{
    public string Scraper { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool UsedStorefrontPassword { get; set; }
    public int Pages { get; set; }
    public int TotalProducts { get; set; }
    public int TotalVariants { get; set; }

    /// <summary>Left-hand side of the category mapping.</summary>
    public List<ValueCount> ProductTypes { get; set; } = new();
    public List<ValueCount> Tags { get; set; } = new();
    public List<ValueCount> Vendors { get; set; } = new();
    /// <summary>Which option carries the size, e.g. "Size" / "Title".</summary>
    public List<ValueCount> OptionNames { get; set; } = new();
    /// <summary>Left-hand side of the size mapping.</summary>
    public List<ValueCount> Option1Values { get; set; } = new();

    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }
    public decimal PriceAverage { get; set; }
    public int VariantsWithCompareAtPrice { get; set; }
    public int ProductsWithDescription { get; set; }
    public int ProductsWithNoImages { get; set; }

    public List<CatalogSampleProduct> Samples { get; set; } = new();
}

public class ValueCount
{
    public string Value { get; set; } = "";
    public int Count { get; set; }
}

public class CatalogSampleProduct
{
    public string Title { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string ProductType { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> OptionNames { get; set; } = new();
    public List<string> Sizes { get; set; } = new();
    public string Price { get; set; } = "";
    public string? CompareAtPrice { get; set; }
    public int Images { get; set; }
    public bool HasDescription { get; set; }
}
