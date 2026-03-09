using Amazon.S3.Model;
using CMS_Scrappers.BackgroundJobs.Cronjobs.Interface;
using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;
namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

public class Scrappingjob:BackgroundService
{
    private readonly IHighPriorityTaskQueue _taskQueue;
    private readonly TimeSpan __interval = TimeSpan.FromHours(3);
    private readonly ILogger<Scrappingjob> _logger;
    public Scrappingjob(IHighPriorityTaskQueue taskQueue,ILogger<Scrappingjob> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
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