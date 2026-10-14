namespace CMS_Scrappers.Utils;

/// <summary>
/// Settings for the job that compares Shopify's real stock with the supplier stock in our DB
/// and pushes the difference. Bound from the "StockReconcile" section of appsettings.
/// </summary>
public class StockReconcileSettings
{
    /// <summary>Turn off with StockReconcile__Enabled=false if the job ever needs to be paused.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the job runs. The scrape runs every 3h; this catches what the scrape cannot see.</summary>
    public int IntervalHours { get; set; } = 6;

    /// <summary>
    /// Safety guard: if a run would set more than this many variants to 0 on one store it aborts,
    /// so a broken scrape cannot wipe stock. The manual endpoint can pass force=true to override.
    /// </summary>
    public int MaxZeroOutPerRun { get; set; } = 300;

    /// <summary>Name of the scraper row to wait on before running (matches Scrappers.Name).</summary>
    public string ScraperName { get; set; } = "Savonches";
}
