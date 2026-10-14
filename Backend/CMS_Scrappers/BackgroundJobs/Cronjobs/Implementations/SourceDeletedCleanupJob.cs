using CMS_Scrappers.BackgroundJobs.Cronjobs.Interface;
using CMS_Scrappers.Coordinators.Interfaces;

namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

public class SourceDeletedCleanupJob:BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SourceDeletedCleanupJob> _logger;
    
    public SourceDeletedCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<SourceDeletedCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
      
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
           
            // Run every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            try
            {
                //await Cleanupjob();

                _logger.LogInformation("Source deleted cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Source deleted cleanup failed");
            }
        }
    }

    private async Task Cleanupjob()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var Shopifyscrfactory = scope.ServiceProvider.GetRequiredService<IShopifyScrapperFact>();
            var _productSyncCoordinator = scope.ServiceProvider.GetRequiredService<IProductSyncCoordinator>();
            var _rsyncCoordinator = scope.ServiceProvider.GetRequiredService<IRRsyncCoordinator>();
            var scraper = Shopifyscrfactory.CreateScraper("savonches");
            await scraper.MarkUnseenProductsAsSourceDeleted();
            await _productSyncCoordinator.CleanupSourceDeletedFromShopify();
            await _rsyncCoordinator.DeleteStaleFromRRSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup job failed to resolve services");
            // Log inner exceptions
            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    _logger.LogError(inner, "Inner: {Message}", inner.Message);
                }
            }
        }
    } 
}