namespace Watts.Azure.Common.DataFactory.General
{
    using System;
    using System.Linq;
    using Microsoft.Azure.Management.DataFactories.Models;
    using Watts.Azure.Common.Interfaces.DataFactory;
    using Watts.Azure.Common.Interfaces.Storage;

    public class LinkedServiceHelper
    {
        private Action<string> progressDelegate;

        public LinkedServiceHelper(Action<string> progressDelegate)
        {
            this.progressDelegate = progressDelegate;
        }

        public void CreateTargetIfItDoesntExist(IAzureLinkedService sourceService, IAzureLinkedService targetService)
        {
            if (sourceService is IAzureTableStorage && targetService is IAzureTableStorage)
            {
                this.CreateTargetIfItDoesntExist(sourceService as IAzureTableStorage, targetService as IAzureTableStorage);
                return;
            }
            else if (sourceService is IAzureTableStorage && targetService is IAzureDataLakeStore)
            {
                IAzureDataLakeStore dataLake = targetService as IAzureDataLakeStore;
                dataLake.CreateDirectory(string.Empty);
                return;
            }

            throw new NotImplementedException($"Creation of {targetService.GetType().Name} from {sourceService.GetType().Name} has not been implemented yet...");
        }

        public void CreateTargetTableIfNotExists(IAzureTableStorage sourceTable, IAzureTableStorage targetTable)
        {
            var tableReference = targetTable.GetTableReference();

            if (!tableReference.Exists())
            {
                this.progressDelegate?.Invoke($"Target table did not already exist. Creating table {targetTable.Name}");
                var exampleEntities = sourceTable.GetTop(10);

                var templateEntity =
                    exampleEntities.FirstOrDefault(
                        e => e.Properties.Count() == exampleEntities.Max(p => p.Properties.Count()));

                targetTable.CreateTableFromTemplateEntity(templateEntity);
            }
        }

        public LinkedServiceTypeProperties GetLinkedServiceTypeProperties(IAzureLinkedService linkedService)
        {
            if (linkedService is IAzureTableStorage)
            {
                return new AzureStorageLinkedService(linkedService.ConnectionString);
            }
            else if (linkedService is IAzureDataLakeStore)
            {
                IAzureDataLakeStore dataLake = linkedService as IAzureDataLakeStore;

                return new AzureDataLakeStoreLinkedService()
                {
                    AccountName = dataLake.Name,
                    DataLakeStoreUri = dataLake.ConnectionString,
                    SubscriptionId = dataLake.Authenticator.SubscriptionId,
                    ServicePrincipalId = dataLake.Authenticator.Credentials.ClientId,
                    ServicePrincipalKey = dataLake.Authenticator.Credentials.ClientSecret,
                    Tenant = dataLake.Authenticator.Credentials.TenantId,
                    ResourceGroupName = dataLake.Authenticator.ResourceGroupName
                };
            }

            throw new NotImplementedException($"GetLinkedServiceTypeProperties does not have an implementation for type {linkedService.GetType().Name}");
        }
    }
}