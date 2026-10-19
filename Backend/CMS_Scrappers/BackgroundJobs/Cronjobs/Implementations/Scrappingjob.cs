using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.Scraping;
using CMS_Scrappers.Services.Interfaces;

namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Implementations;

/// <summary>Every 3 hours: refresh proxies, then let the orchestrator run every enabled supplier in turn.</summary>
public class Scrappingjob : BackgroundService
{
    private readonly IHighPriorityTaskQueue _taskQueue;
    private readonly TimeSpan _interval = TimeSpan.FromHours(3);
    private readonly ILogger<Scrappingjob> _logger;
    private readonly IProxyManager _proxyManager;

    public Scrappingjob(IHighPriorityTaskQueue taskQueue, ILogger<Scrappingjob> logger, IProxyManager proxyManager)
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
                    var orchestrator = serviceProvider.GetRequiredService<IScrapeOrchestrator>();
                    var results = await orchestrator.RunAllEnabledAsync(token);
                    _logger.LogInformation("Scheduled scrape cycle done: {Ok} ok, {Failed} failed, {Skipped} skipped",
                        results.Count(r => r.Succeeded), results.Count(r => r.Ran && !r.Succeeded), results.Count(r => !r.Ran));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue the scheduled scrape cycle");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
