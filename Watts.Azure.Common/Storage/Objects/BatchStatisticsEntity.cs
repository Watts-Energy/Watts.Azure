namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// An entity containing details about a batch execution (length, which pool and job id, how many nodes, etc).
    /// </summary>
    public class BatchStatisticsEntity : TableEntity
    {
        public BatchStatisticsEntity(string poolId, string jobId, DateTimeOffset startTime, DateTimeOffset endTime, int numberOfNodes, int numberOfTasks, string machineSize, string metadata, string type)
        {
            this.PartitionKey = poolId;
            this.RowKey = Guid.NewGuid().ToString();

            this.PoolId = poolId;
            this.JobId = jobId;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.NumberOfNodes = numberOfNodes;
            this.NumberOfTasks = numberOfTasks;
            this.MachineSize = machineSize;
            this.Metadata = metadata;
            this.Type = type;
        }

        public string JobId { get; set; }

        public string PoolId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public int NumberOfNodes { get; set; }

        public int NumberOfTasks { get; set; }

        public string MachineSize { get; set; }

        public string Metadata { get; set; }

        public string Type { get; set; }
    }
}