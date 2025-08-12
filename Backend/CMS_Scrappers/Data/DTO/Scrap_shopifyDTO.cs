public class ShopifyFlatProduct
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Handle { get; set; }

    // Reflect your schema exactly: complex types for images and variants
    public List<ProductImageRecordDTO> Images { get; set; } = new();
    public List<ProductVariantRecordDTO> Variants { get; set; } = new();

    // Optional fields from Sdata
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? ProductUrl { get; set; }
    public decimal? Price { get; set; }
    public decimal? Retail_Price { get; set; }  
    public string? Category { get; set; }
    public string? ProductType {get;set;}
    public string? Gender { get; set; }
    public string Condition { get; set; }
    public string ConditionGrade { get; set; }
    public bool Enriched { get; set; } = false;
    public string? ScraperName { get; set; }
    public string? Status { get; set; }
    public string? StatusDulicateId { get; set; }
    public string? DuplicateSource { get; set; }
    public string? Hashimg { get; set; }
    // If you don't use Hashtext in DB, can omit this or keep as needed
    public string? Hashtext { get; set; }
    public bool New { get; set; }
}

public class ProductImageRecordDTO
{
    public string? Id { get; set; }
    public int Priority { get; set; }
    public string Url { get; set; } = "";
    public bool ?Bgremove { get; set; }=false;


}

public class ProductVariantRecordDTO
{
    public string Size { get; set; } = "";
    public string SKU { get; set; } = "";
    public decimal Price { get; set; }=0;
    
    public int Available{get;set;}
}
