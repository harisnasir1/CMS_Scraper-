using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

/// <summary>
/// Runs once a day at a fixed UTC hour and promotes "Categorized" products to "Live"
/// using the rules in <see cref="AutoApproveSettings"/>. Independent of the scrape job.
/// </summary>
public class AutoApproveJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoApproveJob> _logger;
    private readonly AutoApproveSettings _settings;

    private const int ScraperWaitAttempts = 6;
    private static readonly TimeSpan ScraperWaitInterval = TimeSpan.FromMinutes(5);

    public AutoApproveJob(
        IServiceProvider serviceProvider,
        ILogger<AutoApproveJob> logger,
        AutoApproveSettings settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("AutoApprove job is disabled by configuration");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(DateTime.UtcNow, _settings.RunAtUtcHour);
            _logger.LogInformation("AutoApprove next run in {Delay} (at {Hour:00}:00 UTC)", delay, _settings.RunAtUtcHour);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await RunOnce();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoApprove run failed");
            }
        }
    }

    private async Task RunOnce()
    {
        using var scope = _serviceProvider.CreateScope();
        var scrapperRepository = scope.ServiceProvider.GetRequiredService<IScrapperRepository>();
        var runner = scope.ServiceProvider.GetRequiredService<IAutoApproveRunner>();

        var scraperId = await scrapperRepository.Giveidbyname(_settings.ScraperName);
        if (scraperId == Guid.Empty)
        {
            _logger.LogError("AutoApprove: scraper '{Name}' not found", _settings.ScraperName);
            return;
        }

        if (!await WaitForScraperToFinish(scrapperRepository, scraperId))
        {
            _logger.LogError("AutoApprove: scraper still running after {Minutes} min, skipping this run",
                ScraperWaitAttempts * ScraperWaitInterval.TotalMinutes);
            return;
        }

        _logger.LogInformation("AutoApprove run started for scraper {Name}", _settings.ScraperName);
        var approved = await runner.RunAsync(scraperId, _settings.MaxProductsPerRun);
        _logger.LogInformation("AutoApprove run finished: {Count} products went Live", approved);

        if (_settings.SyncShopifyAfterApprove && approved > 0)
        {
            await SyncShopifyStores(scope.ServiceProvider);
        }
    }

    private async Task<bool> WaitForScraperToFinish(IScrapperRepository scrapperRepository, Guid scraperId)
    {
        for (var attempt = 1; attempt <= ScraperWaitAttempts; attempt++)
        {
            var scraper = await scrapperRepository.Getscrapeid(scraperId);
            if (scraper == null || scraper.Status != "Running") return true;

            _logger.LogInformation("AutoApprove: scraper running, waiting {Interval} ({Attempt}/{Max})",
                ScraperWaitInterval, attempt, ScraperWaitAttempts);
            await Task.Delay(ScraperWaitInterval);
        }

        var final = await scrapperRepository.Getscrapeid(scraperId);
        return final == null || final.Status != "Running";
    }

    private async Task SyncShopifyStores(IServiceProvider provider)
    {
        var shopifyRepository = provider.GetRequiredService<IShopifyRepository>();
        var syncCoordinator = provider.GetRequiredService<IProductSyncCoordinator>();

        var stores = await shopifyRepository.GiveallStoresToSync();
        foreach (var store in stores)
        {
            try
            {
                var ok = await syncCoordinator.BulkSyncLiveProduct(store.Id);
                _logger.LogInformation("AutoApprove: Shopify sync for {Store} returned {Result}", store.ShopName, ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoApprove: Shopify sync failed for {Store}", store.ShopName);
            }
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTime nowUtc, int runAtUtcHour)
    {
        var hour = Math.Clamp(runAtUtcHour, 0, 23);
        var next = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, hour, 0, 0, DateTimeKind.Utc);
        if (next <= nowUtc) next = next.AddDays(1);
        return next - nowUtc;
    }
}
