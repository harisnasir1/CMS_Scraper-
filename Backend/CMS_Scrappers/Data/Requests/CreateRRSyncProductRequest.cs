namespace CMS_Scrappers.Data.Requests;
using System.Text.Json.Serialization;

public class CreateRRSyncProductRequest
{
    public string ProductName { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Note { get; set; } = "";
    public string StockXId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Gender { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string UrlKey { get; set; } = "";
    public string ThumbnailImage { get; set; } = "";
    public int Rating { get; set; } = 0;
    public string ColorWay { get; set; } = "";
    public List<CreateRRSyncProductImage> Images { get; set; } = new();
}

public class CreateRRSyncProductImage
{
    [JsonPropertyName("image_Url")]
    public string ImageUrl { get; set; } = "";
    public int Position { get; set; } = 0;
    public bool IsRemoveBackground { get; set; } = true;
    public bool IsThumbnail { get; set; } = false;
}

public class BulkCreateRRSyncProductRequest
{
   
    public string SourceId { get; set; }
    public List<CreateRRSyncProductRequest> Products { get; set; }
}