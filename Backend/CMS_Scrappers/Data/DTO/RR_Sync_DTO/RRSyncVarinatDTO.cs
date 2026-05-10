namespace CMS_Scrappers.Data.DTO.RR_Sync_DTO;

public class RRSyncVarinatDTO
{
    public string? Id { get; set; }
    public string ProductId { get; set; } = "";
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string SellerId { get; set; } = "";
    public decimal CostPrice { get; set; } = 0;
    public decimal SellPrice { get; set; } = 0;
    public decimal Fees { get; set; } = 0;
    public decimal Profit { get; set; } = 0;
    public string? SKU { get; set; }
    public string? LocationId { get; set; }
    public int? Quantity { get; set; }
}