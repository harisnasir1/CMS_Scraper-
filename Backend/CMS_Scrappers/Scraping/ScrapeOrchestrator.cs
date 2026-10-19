using System.Collections.Concurrent;
using System.Diagnostics;
using CMS_Scrappers.Suppliers;

namespace CMS_Scrappers.Scraping;

public sealed class ScrapeOrchestrator : IScrapeOrchestrator
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScrapeOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _running = new();

    public ScrapeOrchestrator(IServiceProvider services, ILogger<ScrapeOrchestrator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public bool TryReserve(string scraperName) =>
        _running.TryAdd(SupplierProfile.Normalize(scraperName), DateTime.UtcNow);

    public bool IsRunning(string scraperName) =>
        _running.ContainsKey(SupplierProfile.Normalize(scraperName));

    public IReadOnlyCollection<string> Running => _running.Keys.ToList();

    public async Task<ScrapeRunResult> RunOneAsync(string scraperName, CancellationToken ct = default)
    {
        var key = SupplierProfile.Normalize(scraperName);
        if (!_running.ContainsKey(key))
            throw new InvalidOperationException($"Call TryReserve(\"{scraperName}\") before RunOneAsync");

        var sw = Stopwatch.StartNew();
        try
        {
            using var scope = _services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IShopifyScrapperFact>();
            var scraper = await factory.CreateScraperAsync(scraperName);

            _logger.LogInformation("Orchestrator: scrape {Name} starting", scraperName);
            await scraper.ScrapeAsync();
            _logger.LogInformation("Orchestrator: scrape {Name} finished in {Elapsed}", scraperName, sw.Elapsed);
            return new ScrapeRunResult { Scraper = scraperName, Ran = true, Succeeded = true, Elapsed = sw.Elapsed };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator: scrape {Name} failed after {Elapsed}", scraperName, sw.Elapsed);
            return new ScrapeRunResult { Scraper = scraperName, Ran = true, Succeeded = false, Error = ex.Message, Elapsed = sw.Elapsed };
        }
        finally
        {
            _running.TryRemove(key, out _);
        }
    }

    public async Task<List<ScrapeRunResult>> RunAllEnabledAsync(CancellationToken ct = default)
    {
        List<SupplierProfile> enabled;
        using (var scope = _services.CreateScope())
        {
            enabled = await scope.ServiceProvider.GetRequiredService<ISupplierProfileProvider>().GetEnabledAsync();
        }

        if (enabled.Count == 0)
        {
            _logger.LogWarning("Orchestrator: no enabled suppliers; nothing to scrape");
            return new List<ScrapeRunResult>();
        }

        _logger.LogInformation("Orchestrator: {Count} enabled supplier(s): {Names}",
            enabled.Count, string.Join(", ", enabled.Select(p => p.Name)));

        var results = new List<ScrapeRunResult>(enabled.Count);
        foreach (var profile in enabled)
        {
            if (ct.IsCancellationRequested) break;

            if (!TryReserve(profile.Name))
            {
                _logger.LogWarning("Orchestrator: {Name} is already queued or running; skipping this cycle", profile.Name);
                results.Add(new ScrapeRunResult { Scraper = profile.Name, SkippedReason = "already running" });
                continue;
            }

            // Sequential on purpose: proxies are shared and each scrape ends with RRSync/Shopify syncs
            results.Add(await RunOneAsync(profile.Name, ct));
        }
        return results;
    }
}
