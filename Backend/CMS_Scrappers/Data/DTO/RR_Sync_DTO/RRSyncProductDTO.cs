namespace CMS_Scrappers.Data.DTO.RR_Sync_DTO;

public class RRSyncProductDTO
{
    public string Name { get; set; } = "";
    public string SKU { get; set; } = "";
    public string? Note { get; set; }
    public string? StockXId { get; set; }
    public string? Colorway { get; set; }
    public string? CategoryId { get; set; }
    public string? SubcategoryId { get; set; }
    public string? Gender { get; set; }
    public string? UrlKey { get; set; }
    public string ProductBrandId { get; set; } = "";
    public string ProductTypeId { get; set; } = "";
    public int Rating { get; set; } = 0;
    public string ThumbnailImage { get; set; } = "";
    public string? SourceId { get; set; }
    public List<CreateRRSyncProductImage> ProductImages { get; set; } = new();
}
public class CreateRRSyncProductImage
{
    public string ImageUrl { get; set; } = "";
    public int Position { get; set; } = 0;
    public bool IsRemoveBackground { get; set; } = true;
    public bool IsThumbnail { get; set; } = false;
}