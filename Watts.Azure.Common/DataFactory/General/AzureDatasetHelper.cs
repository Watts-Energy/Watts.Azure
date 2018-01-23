namespace Watts.Azure.Common.DataFactory.General
{
    using System;
    using Microsoft.Azure.Management.DataFactories.Models;
    using Watts.Azure.Common.Interfaces.DataFactory;
    using Watts.Azure.Common.Interfaces.Storage;

    public class AzureDatasetHelper
    {
        public DatasetTypeProperties GetTypeProperties(IAzureLinkedService service, string targetName = null)
        {
            if (service is IAzureTableStorage)
            {
                return new AzureTableDataset()
                {
                    TableName = service.Name,
                };
            }
            else if (service is IAzureDataLakeStore)
            {
                var dataLake = service as IAzureDataLakeStore;

                return new AzureDataLakeStoreDataset()
                {
                    FolderPath = dataLake.Directory,
                    FileName = targetName,
                    Format = new TextFormat()
                    {
                        FirstRowAsHeader = true,
                        ColumnDelimiter = "\t",
                    }
                };
            }

            throw new NotImplementedException($"Getting type properties is not implemented for {service.GetType().Name}");
        }

        public CopySource GetCopySource(IAzureLinkedService service)
        {
            if (service is IAzureTableStorage)
            {
                AzureTableSource tableSource = new AzureTableSource() { };
                return tableSource;
            }
            else if (service is IAzureDataLakeStore)
            {
                AzureDataLakeStoreSource dataLakeSource = new AzureDataLakeStoreSource()
                {
                };

                return dataLakeSource;
            }

            throw new NotImplementedException($"Getting copy source has not been implemented for {service.GetType().Name}");
        }

        public CopySink GetCopySink(IAzureLinkedService service)
        {
            if (service is IAzureTableStorage)
            {
                AzureTableSink tableSink = new AzureTableSink()
                {
                    AzureTablePartitionKeyName = "PartitionKey",
                    AzureTableRowKeyName = "RowKey",
                    AzureTableInsertType = "Replace"
                };

                return tableSink;
            }
            else if (service is IAzureDataLakeStore)
            {
                AzureDataLakeStoreSink dataLakeSink = new AzureDataLakeStoreSink()
                {
                };

                return dataLakeSink;
            }

            throw new NotImplementedException($"Getting copy sink has not been implemented for {service.GetType().Name}");
        }

        public void SetSourceQuery(CopySource source, string query)
        {
            if (source is AzureTableSource)
            {
                ((AzureTableSource)source).AzureTableSourceQuery = query;
            }
        }
    }
}