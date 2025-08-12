using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
public class ProductImageRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public Guid SdataId { get; set; }
    [JsonIgnore]
    [ForeignKey("SdataId")]
    public Sdata Sdata { get; set; }
    public Guid ProductId { get; set; }
    public int? Priority { get; set; }
    public string Url { get; set; }
}