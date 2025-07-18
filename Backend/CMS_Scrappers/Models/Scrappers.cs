using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Scrapper{
    [Key]
    public Guid ID {get;set;}
    
    public Guid Uid {get;set;}
    [ForeignKey("Uid")]    
    public User User {get;set;}

    public string Name {get;set;}="";
    public string Baseurl {get;set;}="";
    public DateTime Lastrun {get;set;}=DateTime.UtcNow;
    public string Status{get;set;}="";
}