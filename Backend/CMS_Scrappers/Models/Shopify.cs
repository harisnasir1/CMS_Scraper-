 using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Shopify{

    [Key]
    public Guid Id {get;set;}
    
    
    public Guid Uid {get;set;}
    [ForeignKey("Uid")]
    public User User {get;set;}
    public string ShopName{get;set;}="";
    
    public string ApiSecretKey {get;set;}="";
    public string  AdminApiAccessToken {get;set;}="";
    public string HostName{get;set;}="";
}