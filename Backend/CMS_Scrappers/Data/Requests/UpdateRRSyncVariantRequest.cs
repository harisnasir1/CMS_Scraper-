namespace CMS_Scrappers.Data.Requests;

public class UpdateRRSyncVariantRequest
{
    public string Id { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Profit { get; set; }
    public decimal CostPrice { get; set; }
}