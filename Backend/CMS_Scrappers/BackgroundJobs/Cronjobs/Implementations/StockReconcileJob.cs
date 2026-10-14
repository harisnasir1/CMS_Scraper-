using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

/// <summary>
/// Periodically makes every Shopify store's stock match the supplier stock in our DB,
/// using Shopify's real quantities as the baseline. Independent of the scrape job, so it
/// also covers products the supplier has removed (which the scrape never sees again).
/// </summary>
public class StockReconcileJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockReconcileJob> _logger;
    private readonly StockReconcileSettings _settings;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);
    private const int ScraperWaitAttempts = 6;
    private static readonly TimeSpan ScraperWaitInterval = TimeSpan.FromMinutes(5);

    public StockReconcileJob(IServiceProvider serviceProvider, ILogger<StockReconcileJob> logger, StockReconcileSettings settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("StockReconcile job is disabled by configuration");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _settings.IntervalHours));
        try
        {
            // Let the boot-time scrape get going first
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StockReconcile run failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var scrapperRepository = scope.ServiceProvider.GetRequiredService<IScrapperRepository>();
        var reconciler = scope.ServiceProvider.GetRequiredService<IStockReconciler>();

        var scraperId = await scrapperRepository.Giveidbyname(_settings.ScraperName);
        if (scraperId != Guid.Empty && !await WaitForScraperToFinish(scrapperRepository, scraperId))
        {
            _logger.LogWarning("StockReconcile: scraper still running after {Minutes} min, skipping this run",
                ScraperWaitAttempts * ScraperWaitInterval.TotalMinutes);
            return;
        }

        _logger.LogInformation("StockReconcile run started");
        var results = await reconciler.ReconcileAllStoresAsync(dryRun: false, force: false, ct);
        foreach (var r in results)
        {
            if (r.Aborted)
                _logger.LogError("StockReconcile {Store}: aborted - {Reason}", r.Store, r.AbortReason);
            else
                _logger.LogInformation("StockReconcile {Store}: checked {Checked}, fixed {Fixed}, failed {Failed}, missing {Missing}",
                    r.Store, r.Checked, r.Fixed, r.Failed, r.Missing);
        }
    }

    private async Task<bool> WaitForScraperToFinish(IScrapperRepository scrapperRepository, Guid scraperId)
    {
        for (var attempt = 1; attempt <= ScraperWaitAttempts; attempt++)
        {
            var scraper = await scrapperRepository.Getscrapeid(scraperId);
            if (scraper == null || scraper.Status != "Running") return true;

            _logger.LogInformation("StockReconcile: scraper running, waiting {Interval} ({Attempt}/{Max})",
                ScraperWaitInterval, attempt, ScraperWaitAttempts);
            await Task.Delay(ScraperWaitInterval);
        }

        var final = await scrapperRepository.Getscrapeid(scraperId);
        return final == null || final.Status != "Running";
    }
}
