using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Headers;

public class Sdata
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Uid { get; set; }
    [ForeignKey("Uid")]
    public User User { get; set; }
    public Guid Sid{get;set;}
    [ForeignKey("Sid")]
    public  Scrapper Scr{get;set;}
    public string Title { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Image { get; set; } = "";
    public string Description { get; set; } = "";
    public string ProductUrl {get;set;}="";
    public int Price { get; set; } = 0;
    public int Sellprice { get; set; } = 0;
    public string Category { get; set; } = "";
    public string Gender { get; set; } = "";
    public string ScraperName { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDulicateId { get; set; } = "";
    public string DuplicateSource { get; set; } = "";
    public string Hashtext { get; set; } = "";
    public string Hashimg { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}