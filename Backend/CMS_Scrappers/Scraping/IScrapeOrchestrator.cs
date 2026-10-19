namespace CMS_Scrappers.Scraping;

public sealed class ScrapeRunResult
{
    public string Scraper { get; init; } = "";
    public bool Ran { get; init; }
    public bool Succeeded { get; init; }
    public string? SkippedReason { get; init; }
    public string? Error { get; init; }
    public TimeSpan Elapsed { get; init; }
}

/// <summary>
/// Decides which suppliers run and when. One in-process slot per supplier: a supplier can never
/// scrape twice at once, whether triggered by the schedule or by hand. Singleton.
/// </summary>
public interface IScrapeOrchestrator
{
    /// <summary>Claims the supplier's slot. False when it is already queued or running.</summary>
    bool TryReserve(string scraperName);
    bool IsRunning(string scraperName);
    IReadOnlyCollection<string> Running { get; }

    /// <summary>Runs one supplier. The caller must hold the reservation; it is released when this returns.</summary>
    Task<ScrapeRunResult> RunOneAsync(string scraperName, CancellationToken ct = default);

    /// <summary>Runs every enabled supplier one after another, in name order, skipping any whose slot is taken.</summary>
    Task<List<ScrapeRunResult>> RunAllEnabledAsync(CancellationToken ct = default);
}
