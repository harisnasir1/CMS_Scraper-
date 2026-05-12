namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses.RRSyncResopnse;

public class RRSyncProductVaraintResponse
{
    public string Id { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? SKU { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Profit { get; set; }
    public string SellerId { get; set; }
    public string? LocationId { get; set; }
    public string BusinessName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsLive { get; set; }
    public int Quantity { get; set; }
}