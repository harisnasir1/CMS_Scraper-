using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace CMS_Scrappers.Models;

public class VariantStoreMapping
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public long VariantId { get; set; }
    [JsonIgnore]
    [ForeignKey("VariantId")]
    public ProductVariantRecord Variant { get; set; }

    public Guid ProductStoreMappingId { get; set; }
    [JsonIgnore]
    [ForeignKey("ProductStoreMappingId")]
    public ProductStoreMapping ProductStoreMapping { get; set; }

    public string ShopifyVariantId { get; set; } = "";

    public string ShopifyInventoryId { get; set; } = "";

    [Column(TypeName = "decimal(10,2)")]
    public decimal ShopifyPrice { get; set; }

    public bool InStock { get; set; } = false; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}