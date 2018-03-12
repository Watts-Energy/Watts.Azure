namespace Watts.Azure.Common.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;

    public class CouldNotUpsertBatchException : Exception
    {
        public CouldNotUpsertBatchException(List<ITableEntity> batch) : base("Unable to upsert a batch of entities. See inner exceptions for partition keys...", new Exception(string.Join(",", batch.Select(p => p.PartitionKey))))
        {
        }
    }
}