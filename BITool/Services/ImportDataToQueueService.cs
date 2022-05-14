using BITool.BackgroundJobs;
using BITool.Models.ImportDataToQueue;
using MySql.Data.MySqlClient;
using System.Data;

namespace BITool.Services
{
    public interface IImportDataToQueueService
    {
        void InsertImportHistory(ImportDataHistory input);
    }

    public class ImportDataToQueueService : IImportDataToQueueService
    {
        private readonly IBackgroundTaskQueue taskQueue;
        private readonly ILogger logger;
        private readonly CancellationToken cancellationToken;
        private readonly IConfiguration configuration;

        public ImportDataToQueueService(
            IBackgroundTaskQueue taskQueue,
            IConfiguration configuration,
            ILogger<ImportDataToQueueService> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            this.taskQueue = taskQueue;
            this.logger = logger;
            this.configuration = configuration;
            cancellationToken = applicationLifetime.ApplicationStopping;
        }

        public void InsertImportHistory(ImportDataHistory input)
        {
            logger.LogInformation($"{nameof(ImportDataToQueueService)} is starting. {nameof(InsertImportHistory)} {nameof(ImplementInsertImportHistory)}");
            Task.Run(async () => await taskQueue.QueueBackgroundWorkItemAsync(ct => ImplementInsertImportHistory(cancellationToken, input)));
        }

        private async ValueTask ImplementInsertImportHistory(CancellationToken token, ImportDataHistory input)
        {
            if (token.IsCancellationRequested) return;
            var guid = Guid.NewGuid().ToString();
            logger.LogInformation("Queued Background Task {Guid} is starting.", guid);
            var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];
            try
            {
                using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
                {
                    mConnection.Open();
                    var values = string.Format("('{0}','{1}', '{2}', {3}, {4});",
                            input.ImportName, input.FileName,
                            input.ImportTime.ToString("yyyy-MM-dd hh:mm:ss"),
                            input.TotalRows,
                            input.TotalErrorRows);

                    var sCommand = $"INSERT IGNORE INTO ImportDataHistory(ImportName, FileName, ImportTime, TotalRows, TotalErrorRows) VALUES {values}";
                    using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            logger.LogInformation("Queued Background Task {Guid} completed", guid);
        }
    }
}