using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Name),IsUnique =true)]
public class Scrapper{
    [Key]
    public Guid ID {get;set;}
    
    public Guid Uid {get;set;}
    [ForeignKey("Uid")]    
    public User User {get;set;}

    public string Name {get;set;}="";
    public string Baseurl {get;set;}="";
    public DateTime Lastrun {get;set;}=DateTime.UtcNow;
    public string? Runtime  {get;set;}="";
    public string Status{get;set;}="";

    // Supplier configuration (columns added by hand on 2026-10-18; see docs of phase A)
    /// <summary>Jobs only run suppliers with Enabled = true.</summary>
    public bool Enabled {get;set;} = true;
    /// <summary>ISO currency of the prices in this supplier's products.json.</summary>
    public string Currency {get;set;} = "USD";
    /// <summary>Minimum in-stock variant price (supplier currency) before a product is first pushed to RRSync.</summary>
    [Column(TypeName = "numeric(10,2)")]
    public decimal MinFirstPushPrice {get;set;} = 500m;
}
