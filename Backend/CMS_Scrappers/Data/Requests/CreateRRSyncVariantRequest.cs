namespace CMS_Scrappers.Data.Requests;

public class CreateRRSyncVariantRequest
{
    public string? Id { get; set; } = "";
    public string Size { get; set; } = "";
    public string Color { get; set; } = "";
    public int Qty { get; set; } = 0;
    public decimal SellPrice { get; set; } = 0;
    public decimal Fees { get; set; } = 0;
    public decimal Profit { get; set; } = 0;
    public decimal CostPrice { get; set; } = 0;
    public string? sourcevariantid { get; set; }
}