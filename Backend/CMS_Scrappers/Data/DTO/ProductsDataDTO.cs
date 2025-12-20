namespace CMS_Scrappers.Data.DTO;

public class ProductStoreMappingDTO
{
    public Guid ProductId { get; set; }
    public Guid ShopifyStoreId { get; set; }
    public string ExternalProductId { get; set; }
    public string SyncStatus { get; set; }
    public DateTime LastSyncedAt { get; set; }
}