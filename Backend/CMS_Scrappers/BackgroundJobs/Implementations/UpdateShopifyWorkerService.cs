using CMS_Scrappers.BackgroundJobs.Interfaces;

namespace CMS_Scrappers.BackgroundJobs.Implementations
{
    public class UpdateShopifyWorkerService : BackgroundService
    {
        private readonly IUpdateShopifyTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateShopifyTaskQueue> _logger;
        public UpdateShopifyWorkerService(IUpdateShopifyTaskQueue taskQueue, IServiceProvider serviceProvider, ILogger<UpdateShopifyTaskQueue> logger)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Update Shopify Queued Processor Background Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(cancellationToken);

                try
                {
                    await workItem(_serviceProvider, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                }
            }

            _logger.LogInformation("Update Shopify Queued Processor Background Service is stopping.");
        }
    }
}
