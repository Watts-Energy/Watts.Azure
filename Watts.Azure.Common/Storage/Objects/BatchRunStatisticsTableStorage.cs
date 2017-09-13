namespace Watts.Azure.Common.Storage.Objects
{
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

        public void SaveStatistic(BatchStatisticsEntity statistic)
        {
            this.Insert(statistic);
        }
    }
}