namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses.RRSyncResopnse;

public class RRSyncSourceResponse
{
    public string Name { get; set; } = string.Empty;
    
    public string Id { get; set; }
    
    public string CreatedBy { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public string? ModifiedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; }
    
    public bool IsActive { get; set; }
    
    public bool IsDeleted { get; set; }
}