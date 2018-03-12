namespace Watts.Azure.Common.Storage.Objects
{
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;

    /// <summary>
    /// Table storage containing statistics about batch executions.
    /// </summary>
    public class BatchRunStatisticsTableStorage : AzureTableStorage
    {
        public BatchRunStatisticsTableStorage(CloudStorageAccount storageAccount, string tableName = "BatchStatistics")
            : base(storageAccount, tableName)
        {
            this.Name = tableName;
        }

        public void SaveStatistics(BatchStatisticsEntity statistic)
        {
            this.Insert(statistic);
        }

        public async Task SaveStatisticsAsync(BatchStatisticsEntity statistics)
        {
            await this.InsertAsync(statistics);
        }
    }
}