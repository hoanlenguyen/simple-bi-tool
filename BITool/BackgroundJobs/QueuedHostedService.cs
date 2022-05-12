namespace BITool.BackgroundJobs
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> logger;
        public IBackgroundTaskQueue taskQueue { get; }

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger)
        {
            this.taskQueue = taskQueue;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Queued Hosted Service is running.");
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Queued Hosted Service is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}