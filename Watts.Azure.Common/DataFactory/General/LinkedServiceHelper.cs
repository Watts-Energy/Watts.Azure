namespace Watts.Azure.Common.DataFactory.General
{
    using System;
    using System.Linq;
    using Watts.Azure.Common.Interfaces.DataFactory;
    using Watts.Azure.Common.Interfaces.Storage;

    public class LinkedServiceHelper
    {
        public Action<string> progressDelegate;

        public LinkedServiceHelper(Action<string> progressDelegate)
        {
            this.progressDelegate = progressDelegate;
        }

        public void CreateTargetIfItDoesntExist(IAzureLinkedService sourceService, IAzureLinkedService targetService)
        {
            if(sourceService is IAzureTableStorage && targetService is IAzureTableStorage)
            {
                this.CreateTargetIfItDoesntExist(sourceService as IAzureTableStorage, targetService as IAzureTableStorage);
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
    }
}