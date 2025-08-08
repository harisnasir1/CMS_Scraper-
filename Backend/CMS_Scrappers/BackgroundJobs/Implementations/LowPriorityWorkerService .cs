using CMS_Scrappers.BackgroundJobs.Interfaces;



    public class LowPriorityWorkerService:BackgroundService
    {
        private readonly ILowPriorityTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        
        public LowPriorityWorkerService(ILowPriorityTaskQueue taskQueue, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<LowPriorityWorkerService>();
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Low pirority Queued Processor Background Service is starting.");

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

            _logger.LogInformation("Low pirority Queued Processor Background Service is stopping.");
        }
    }

