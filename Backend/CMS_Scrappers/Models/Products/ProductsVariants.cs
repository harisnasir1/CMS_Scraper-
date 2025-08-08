using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
public class ProductVariantRecord
{
    public long Id { get; set; }
    public Guid SdataId { get; set; }
    [JsonIgnore]
    [ForeignKey("SdataId")]
    public Sdata Sdata { get; set; }

    public string Size { get; set; } = "";

    public string SKU { get; set; } = "";

    public decimal Price { get; set; }

    public bool InStock { get; set; }
}