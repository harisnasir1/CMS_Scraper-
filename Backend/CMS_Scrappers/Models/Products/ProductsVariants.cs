using System.ComponentModel.DataAnnotations.Schema;
public class ProductVariantRecord
{
    public long Id { get; set; }
    public Guid SdataId { get; set; }  
    
    [ForeignKey("SdataId")]
    public Sdata Sdata { get; set; }

    public Guid ProductId { get; set; }

    public string Size { get; set; } = "";

    public string SKU { get; set; } = "";

    public decimal Price { get; set; }

    public bool InStock { get; set; }
}