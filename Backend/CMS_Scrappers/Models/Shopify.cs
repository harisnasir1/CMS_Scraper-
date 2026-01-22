using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Shopify{

    [Key]
    public Guid Id {get;set;}   
    
    public string ShopName{get;set;}="";
    
    public string ApiSecretKey {get;set;}="";

    public string ApiKey { get; set; } = "";
    public string  AdminApiAccessToken {get;set;}="";
    
    public string HostName{get;set;}="";
    
    public int VariantsCreatedToday { get; set; } = 0;

    public DateTime? LastSyncedOn { get; set; }= DateTime.UtcNow.Date;
    
    public DateTime LastVariantResetDate { get; set; } = DateTime.UtcNow.Date;
}