using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using CMS_Scrappers.Suppliers;

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
    private static readonly TimeSpan ScraperWaitInterval = TimeSpan.FromMinutes(15);

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
        var profiles = scope.ServiceProvider.GetRequiredService<ISupplierProfileProvider>();

        var enabled = await profiles.GetEnabledAsync();
        if (enabled.Count == 0)
        {
            _logger.LogWarning("AutoApprove: no enabled suppliers");
            return;
        }

        var totalApproved = 0;
        foreach (var profile in enabled)
        {
            if (!await WaitForScraperToFinish(scrapperRepository, profile.Id))
            {
                _logger.LogError("AutoApprove: {Name} still scraping after {Minutes} min, skipping it this run",
                    profile.Name, ScraperWaitAttempts * ScraperWaitInterval.TotalMinutes);
                continue;
            }

            // Reserve after the wait, not before, so a long scrape does not keep manual runs rejected for its whole duration
            if (!runner.tryBegin())
            {
                _logger.LogWarning("AutoApprove: skipping {Name}, a manual run is queued or running", profile.Name);
                continue;
            }

            _logger.LogInformation("AutoApprove run started for scraper {Name}", profile.Name);
            var approved = await runner.RunAsync(profile.Id, _settings.MaxProductsPerRun);
            _logger.LogInformation("AutoApprove run finished for {Name}: {Count} products went Live", profile.Name, approved);
            totalApproved += approved;
        }

        if (_settings.SyncShopifyAfterApprove && totalApproved > 0)
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
