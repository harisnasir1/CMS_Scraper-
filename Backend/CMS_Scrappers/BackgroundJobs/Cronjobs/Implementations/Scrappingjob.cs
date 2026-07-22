using Amazon.S3.Model;
using CMS_Scrappers.BackgroundJobs.Cronjobs.Interface;
using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;
using CMS_Scrappers.Services.Interfaces;

namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

public class Scrappingjob:BackgroundService
{
    private readonly IHighPriorityTaskQueue _taskQueue;
    private readonly TimeSpan __interval = TimeSpan.FromHours(3);
    private readonly ILogger<Scrappingjob> _logger;
    private readonly IProxyManager _proxyManager;
    public Scrappingjob(IHighPriorityTaskQueue taskQueue,ILogger<Scrappingjob> logger,IProxyManager proxyManager)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _proxyManager = proxyManager;
    }
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _proxyManager.RefreshProxiesAsync(stoppingToken);
            try
            {
                _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var Shopifyscrfactory = scope.ServiceProvider.GetRequiredService<IShopifyScrapperFact>();
                    var scraper = Shopifyscrfactory.CreateScraper("savonches");
                   await scraper.ScrapeAsync();
                });
            }
            catch (Exception ex)
            {
              _logger.LogError($"an error occur on cron job white scrappign,{ex}");
            }
            await Task.Delay(__interval, stoppingToken);

        }
       
    }
}