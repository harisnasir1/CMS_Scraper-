using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Scrappers.Models;

/// <summary>
/// One row per supplier per external system (Provider). Identity only: the password lives in
/// configuration as Suppliers__&lt;Name&gt;__RRSyncPassword, never in this table.
/// Table "ScraperSyncAccounts", created by hand on 2026-10-18.
/// </summary>
[Table("ScraperSyncAccounts")]
public class ScraperSyncAccount
{
    [Key]
    public Guid Id { get; set; }

    public Guid ScraperId { get; set; }
    [ForeignKey(nameof(ScraperId))]
    public Scrapper? Scraper { get; set; }

    public string Provider { get; set; } = "RRSync";

    /// <summary>Source name on the provider side, e.g. "cms-savonches".</summary>
    public string SourceName { get; set; } = "";

    public string UserName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
