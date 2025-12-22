using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace CMS_Scrappers.Models
{
    public class ProductStoreMapping
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }  
        [ForeignKey("ProductId")]
        public Sdata Product { get; set; }

        public Guid ShopifyStoreId { get; set; }  
        [JsonIgnore]//store id
        [ForeignKey("ShopifyStoreId")]
        public Shopify ShopifyStore { get; set; }

        public string ExternalProductId { get; set; } = "";   //shopifyproductid

        public string SyncStatus { get; set; } = "live";   // live

        public DateTime? LastSyncedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}