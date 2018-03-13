namespace Watts.Azure.Common.Backup
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using DataFactory.Copy;
    using DataFactory.General;
    using Exceptions;
    using Interfaces.Security;
    using Interfaces.Storage;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.Storage.Fluent;
    using Microsoft.WindowsAzure.Storage.Table;
    using Storage.Objects;

    public class TableStorageBackup
    {
        private readonly BackupSetup setup;
        private readonly IAzureActiveDirectoryAuthentication authentication;
        private readonly BackupManagementTable backupManagementTable;

        public TableStorageBackup(BackupSetup setup, IAzureActiveDirectoryAuthentication authentication, BackupManagementTable backupManagementTable)
        {
            this.setup = setup;
            this.authentication = authentication;
            this.backupManagementTable = backupManagementTable;
        }

        public async Task Run()
        {
            // The name of the target storage account is the configured prefix concatenated with the current date.
            string targetAccountName = $"{this.setup.BackupStorageAccountPrefix}{DateTime.Now:yyyyMMdd}";

            var credentials = this.GetCredentials();

            var storageManager = StorageManager.Authenticate(credentials, this.authentication.SubscriptionId);

            // Create the resource group if it doesn't already exist.
            await this.CreateResourceGroupIfDoesntExist(this.setup.BackupTargetResourceGroupName, storageManager);

            //// Create the account if it doesn't exist. 
            //await this.CreateStorageAccountIfNotExists(storageManager, targetAccountName);

            // Go through each table and perform the backup.
            foreach (var tableToBackup in this.setup.TablesToBackup)
            {
                await this.BackupTableAsync(tableToBackup, storageManager);
            }
        }

        internal async Task<IResourceGroup> CreateResourceGroupIfDoesntExist(string resourceGroupName, IStorageManager manager)
        {
            var exists = await manager.ResourceManager.ResourceGroups.ContainAsync(resourceGroupName);

            if (!exists)
            {
                return await manager.ResourceManager.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(this.setup.BackupTargetRegion).CreateAsync();
            }
            else
            {
                return await manager.ResourceManager.ResourceGroups.GetByNameAsync(resourceGroupName);
            }
        }

        /// <summary>
        /// Perform a backup according to the given setup.
        /// </summary>
        /// <param name="tableBackupSetup"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        internal async Task BackupTableAsync(TableBackupSetup tableBackupSetup, IStorageManager manager)
        {
            // Check when the table was last backed up.
            var lastBackupEntity = this.backupManagementTable
                .Query<BackupManagementEntity>(p => p.SourceTableName == tableBackupSetup.SourceStorage.Name)
                .OrderByDescending(p => p.BackupStartedAt)
                .FirstOrDefault();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            IStorageAccount targetAccount;

            string sourceQuery = null;

            if (lastBackupEntity == null)
            {
                // We've never backed this table up before.
                targetAccount = await this.GetAccount(manager, now);
            }
            else
            {
                // If the last backup started more than the configured frequency ago, we should run the backup now.
                var mustRunBackupNow = now - lastBackupEntity.BackupStartedAt >
                                       tableBackupSetup.IncrementalChangesFrequency;

                if (!mustRunBackupNow)
                {
                    return;
                }

                var targetStorageAccountName = lastBackupEntity.TargetStorageAccountName;

                DateTime timeWhenStorageContainerWasCreated =
                    this.GetDateFromStorageAccountName(lastBackupEntity.TargetStorageAccountName);

                // If the last backup is so long ago that we should change storage account target for the table, create a new storage account.
                var mustChangeTarget = now - timeWhenStorageContainerWasCreated > tableBackupSetup.SwitchTargetFrequency;

                if (mustChangeTarget)
                {
                    targetAccount = await this.GetAccount(manager, DateTimeOffset.UtcNow);
                    sourceQuery = null;
                }
                else
                {
                    targetAccount = await this.GetStorageAccount(manager, targetStorageAccountName);

                    if (tableBackupSetup.BackupMode == BackupMode.Incremental)
                    {
                        sourceQuery = $"Timestamp gt datetime'{lastBackupEntity.BackupStartedAt.ToIso8601()}'";
                    }
                }
            }

            // Get the authentication info to connect to the target storage account.
            var storageKeys = await targetAccount.GetKeysAsync();
            var key = storageKeys.First().Value;
            var accountName = targetAccount.Name;

            IAzureTableStorage targetTableStorage = new AzureTableStorage(tableBackupSetup.SourceStorage.Name, this.GetStorageConnectionString(accountName, key));

            // Create a data factory and run the actual copy pipeline.
            CopySetup copySetup = new CopySetup
            {
                DeleteDataFactoryIfExists = true,
                TargetDatasetName = $"{targetTableStorage.Name}-target-dataset",
                SourceDatasetName = $"{targetTableStorage.Name}-source-dataset",
                CopyPipelineName = $"Copy-{targetTableStorage.Name}-{now.Year}-{now.Month}-{now.Day}",
                CreateTargetIfNotExists = true,
                SourceLinkedServiceName = $"{targetTableStorage.Name}-source-service",
                TargetLinkedServiceName = $"{targetTableStorage.Name}-target-service",
                TimeoutInMinutes = tableBackupSetup.TimeoutInMinutes
            };

            CopyDataPipeline copyPipeline =
                CopyDataPipeline.UsingDataFactorySettings(this.setup.DataFactorySetup, copySetup, this.authentication, Console.WriteLine);

            copyPipeline.From(tableBackupSetup.SourceStorage).To(targetTableStorage).UsingSourceQuery(sourceQuery).Start();

            copyPipeline.Start();

        }

        internal async Task<IStorageAccount> GetAccount(IStorageManager manager, DateTimeOffset date)
        {
            string targetAccountName = $"{this.setup.BackupStorageAccountPrefix}{DateTime.Now:yyyyMMdd}";

            return await this.CreateStorageAccountIfNotExists(manager, targetAccountName);
        }

        internal string GetStorageConnectionString(string accountName, string accountKey)
        {
            return $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";
        }

        /// <summary>
        /// Extract the date that a backup storage account was created, based on the naming scheme we use.
        /// </summary>
        /// <param name="storageAccountName"></param>
        /// <returns></returns>
        internal DateTime GetDateFromStorageAccountName(string storageAccountName)
        {
            string[] splitName = storageAccountName.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

            var unexpectedNameException = new UnexpectedStorageAccountNameException($"The account name {storageAccountName} does not follow the format [name]-[year]-[month]-[day].");

            // We expect a minimum length of 4 when splitting on the '-' character, since our naming scheme is 'name-year-month-day'
            int expectedMinimumLength = 4;
            if (splitName.Length < expectedMinimumLength)
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(splitName[1], out var year))
            {
                throw unexpectedNameException;
            }

            if (int.TryParse(splitName[2], out var month))
            {
                throw unexpectedNameException;
            }

            if (int.TryParse(splitName[3], out var day))
            {
                throw unexpectedNameException;
            }

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        internal async Task<IStorageAccount> CreateStorageAccountIfNotExists(IStorageManager storageManager, string targetAccountName)
        {
            var account = await this.GetStorageAccount(storageManager, targetAccountName);

            if (account == null)
            {
                account = await storageManager
                    .StorageAccounts
                    .Define(targetAccountName)
                    .WithRegion(this.setup.BackupTargetRegion)
                    .WithExistingResourceGroup(this.setup.BackupTargetResourceGroupName)
                    .WithOnlyHttpsTraffic()
                    .CreateAsync();
            }

            return account;
        }

        internal async Task<IStorageAccount> GetStorageAccount(IStorageManager storageManager, string name)
        {
            return await storageManager.StorageAccounts.GetByResourceGroupAsync(this.setup.BackupTargetResourceGroupName, name);
        }

        internal AzureCredentials GetCredentials()
        {
            return new AzureCredentials(
                new ServicePrincipalLoginInformation()
                {
                    ClientId = this.authentication.Credentials.ClientId,
                    ClientSecret = this.authentication.Credentials.ClientSecret
                },
                this.authentication.Credentials.TenantId,
                this.setup.AzureEnvironment);
        }
    }
}