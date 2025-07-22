public class ShopifyFlatProduct
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Handle { get; set; }
    public List<string> ImageUrls { get; set; }
    
    // Optional Fields from Sdata (add as nullable/optional)

    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? ProductUrl { get; set; }
    public decimal? Price { get; set; }
    public int? Sellprice { get; set; }
    public string? Category { get; set; }
    public string? Gender { get; set; }
    public string? ScraperName { get; set; }
    public string? Status { get; set; }
    public string? StatusDulicateId { get; set; }
    public string? DuplicateSource { get; set; }
    public string? Hashtext { get; set; }
    public string? Hashimg { get; set; }

    public List<string> Sizes {get;set;}
}
