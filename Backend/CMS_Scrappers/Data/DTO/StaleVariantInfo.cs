namespace CMS_Scrappers.Data.DTO;

public class StaleVariantInfo
{
    public long VariantId { get; set; }
    public string ShopifyVariantId { get; set; } = "";
    public string ShopifyProductId { get; set; } = "";   // ExternalProductId = Shopify product GID
    public Guid ProductStoreMappingId { get; set; }
    public Guid VariantStoreMappingId { get; set; }
    public Guid SdataId { get; set; }
}