namespace Watts.Azure.Common.DataFactory.Copy
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Entity for storing information about a data copy operation that was executed.
    /// </summary>
    public class DataMovementHistoryEntity : TableEntity
    {
        public DataMovementHistoryEntity()
        {
            DateTime now = DateTime.Now;

            this.PartitionKey = $"{now.Year}{now.Month.ToString().PadLeft(2, '0')}";
            this.RowKey = now.ToString("yyyyMMdd-HH.mm.ss");
        }

        public string PipelineName { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime CompletedAt { get; set; }

        public string Status { get; set; }
    }
}