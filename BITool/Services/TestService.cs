using BITool.BackgroundJobs;

namespace BITool.Services
{
    public interface ITestService
    {
        void AddTestServiceToQueue(string input);
    }

    public class TestService: ITestService
    {
        private readonly IBackgroundTaskQueue taskQueue;
        private readonly ILogger logger;
        private readonly CancellationToken cancellationToken;
        private readonly ITestService testService;
        public TestService(
            IBackgroundTaskQueue taskQueue,
            ILogger<TestService> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            this.taskQueue = taskQueue;
            this.logger = logger;
            cancellationToken = applicationLifetime.ApplicationStopping;
        }

        public void AddTestServiceToQueue(string input)
        {
            logger.LogInformation($"{nameof(TestService)} is starting. {nameof(AddTestServiceToQueue)} {nameof(ImplementTestWorkItem)}");
            Task.Run(async () => await taskQueue.QueueBackgroundWorkItemAsync(ct => ImplementTestWorkItem(cancellationToken, input)));
        }         

        private async ValueTask ImplementTestWorkItem(CancellationToken token, string input)
        {
            // Simulate three 5-second tasks to complete
            // for each enqueued work item

            int delayLoop = 0;
            var guid = Guid.NewGuid().ToString();

            logger.LogInformation("{Input} Queued Background Task {Guid} is starting.", input, guid);

            while (!token.IsCancellationRequested && delayLoop < 3)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if the Delay is cancelled
                }

                delayLoop++;

                logger.LogInformation("{Input} Queued Background Task {Guid} is running. "
                                       + "{DelayLoop}/3", input, guid, delayLoop);
            }

            if (delayLoop == 3)
            {
                logger.LogInformation("Queued Background Task {Guid} is complete.", guid);
            }
            else
            {
                logger.LogInformation("Queued Background Task {Guid} was cancelled.", guid);
            }
        }
    }
}
