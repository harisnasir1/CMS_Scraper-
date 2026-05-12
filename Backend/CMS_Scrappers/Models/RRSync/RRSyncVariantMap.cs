using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CMS_Scrappers.Models;
using Microsoft.EntityFrameworkCore;
[Table("RRSyncVariantMap")]
[Index(nameof(Id))]
[Index(nameof(VariantId), IsUnique = true)]
[Index(nameof(RRSyncProductMapId))]
[Index(nameof(SyncStatus))]
public class RRSyncVariantMap
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public long VariantId { get; set; }
    [ForeignKey("VariantId")]
    public ProductVariantRecord Variant { get; set; }

    public Guid RRSyncProductMapId { get; set; }
    [ForeignKey("RRSyncProductMapId")]
    public RRSyncProductMap RRSyncProductMap { get; set; }

    public string RRSyncVariantId { get; set; } = "";

    public string SyncStatus { get; set; } = "Active";
    

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}