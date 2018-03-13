namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Linq;
    using DataFactory.Copy;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// A Azure table storage table containing a log of data movement history.
    /// </summary>
    public class DataMovementHistoryTableStorage
    {
        private readonly string tableName;

        private readonly AzureTableStorage table;

        public DataMovementHistoryTableStorage(string connectionString, string tableName = "DataMovementHistory")
        {
            this.tableName = tableName;

            this.table = AzureTableStorage.Connect(connectionString, this.tableName);
            this.table.CreateTableIfNotExists();
        }

        public void AddHistoryItem(DataMovementHistoryEntity item)
        {
            this.table.Insert(item);
        }

        public DateTimeOffset? GetHighestStartTimeForPipeline(string pipelineName)
        {
            var query = new TableQuery<DataMovementHistoryEntity>().Where(
                    TableQuery.GenerateFilterCondition("PipelineName", QueryComparisons.Equal, pipelineName));

            var entities = this.table.Query(query);

            if (entities.Count == 0)
            {
                return null;
            }

            return entities.Where(q => q.Status.Equals("OK")).Max(p => p.StartedAt);
        }
    }
}