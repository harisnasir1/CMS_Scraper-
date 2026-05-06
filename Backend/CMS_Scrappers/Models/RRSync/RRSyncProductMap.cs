using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CMS_Scrappers.Models;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Id))]
[Index(nameof(SdataId), IsUnique = true)]
[Index(nameof(RRSyncProductId))]
[Index(nameof(SyncStatus))]
public class RRSyncProductMap
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SdataId { get; set; }
    [ForeignKey("SdataId")]
    public Sdata Sdata { get; set; }

    public string RRSyncProductId { get; set; } = "";

    public string SyncStatus { get; set; } = "Active";

    public List<RRSyncVariantMap> VariantMaps { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}