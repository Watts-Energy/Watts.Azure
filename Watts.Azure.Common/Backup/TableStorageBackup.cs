namespace Watts.Azure.Common.Backup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DataFactory.Copy;
    using Exceptions;
    using Interfaces.Security;
    using Interfaces.Storage;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.Storage.Fluent;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Storage.Objects;

    public class TableStorageBackup
    {
        private IStorageManager targetStorageManager;
        private readonly BackupSetup setup;
        private readonly IAzureActiveDirectoryAuthentication backupEnvironmentAuthentication;
        private readonly BackupManagementTable backupManagementTable;

        private Action<string> progressDelegate;

        public TableStorageBackup(BackupSetup setup, IAzureActiveDirectoryAuthentication backupEnvironmentAuthentication, BackupManagementTable backupManagementTable, Action<string> reportProgressOn = null)
        {
            this.setup = setup;
            this.backupEnvironmentAuthentication = backupEnvironmentAuthentication;
            this.backupManagementTable = backupManagementTable;
            this.progressDelegate = reportProgressOn;

            this.Initialize();
        }

        public async Task<IEnumerable<BackupResult>> RunAsync()
        {
            var retVal = new List<BackupResult>();

            DateTime backupStartTime = DateTime.UtcNow;

            // Create the resource group if it doesn't already exist.
            await this.CreateBackupResourceGroupIfDoesntExist(this.setup.BackupTargetResourceGroupName);

            // Go through each table and perform the backup.
            foreach (var tableToBackup in this.setup.TablesToBackup)
            {
                retVal.Add(await this.BackupTableAsync(tableToBackup, backupStartTime));
            }

            return retVal;
        }

        public async Task<IEnumerable<string>> CleanUpOldBackupsAsync()
        {
            List<string> retVal = new List<string>();

            var accounts =
                await this.targetStorageManager.StorageAccounts.ListByResourceGroupAsync(this.setup.BackupTargetResourceGroupName, true);

            DateTime now = DateTime.UtcNow;

            do
            {
                foreach (var account in accounts)
                {
                    List<string> tablesToDelete = new List<string>();

                    var createdDate = this.GetDateFromStorageAccountName(account.Name);

                    // Check each backup setup we have to see if there's anything that
                    // should be deleted in the resource group (expired backups).
                    foreach (var tableBackup in this.setup.TablesToBackup)
                    {
                        // If the retentiontime has passed, add the table name for deletion from the backup
                        // storage account
                        if (now - createdDate > tableBackup.RetentionTime)
                        {
                            // We should delete the table
                            tablesToDelete.Add(tableBackup.SourceStorage.Name);
                        }
                    }

                    // Delete any tables that have expired.
                    await this.DeleteTablesAsync(account, tablesToDelete);

                    // Delete the storage account if there are no more tables in it.
                    var deleted = await this.DeleteStorageAccountIfNoMoreTablesAsync(account);

                    if (deleted)
                    {
                        retVal.Add(account.Name);
                    }
                }
            } while (await accounts.GetNextPageAsync() != null);

            return retVal;
        }

        internal async Task<bool> DeleteStorageAccountIfNoMoreTablesAsync(IStorageAccount account)
        {
            CloudStorageAccount tableAccount = CloudStorageAccount.Parse(this.GetStorageConnectionString(account.Name, (await account.GetKeysAsync()).First().Value));
            CloudTableClient tableClient = tableAccount.CreateCloudTableClient();

            if (!tableClient.ListTables().Any())
            {
                await this.targetStorageManager.StorageAccounts.DeleteByIdAsync(account.Id);
                return true;
            }

            return false;
        }

        internal async Task DeleteTablesAsync(IStorageAccount account, List<string> tableNames)
        {
            try
            {
                var keys = await account.GetKeysAsync();
                var connectionString = this.GetStorageConnectionString(account.Name, keys.First().Value);

                foreach (var tableName in tableNames)
                {
                    IAzureTableStorage tableStorage = new AzureTableStorage(tableName, connectionString);
                    var success = tableStorage.DeleteIfExists();

                    if (!success)
                    {
                        throw new TableDeleteFailedException(tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal void Initialize()
        {
            var backupEnvironmentCredentials = this.GetCredentials();

            this.targetStorageManager = StorageManager.Authenticate(backupEnvironmentCredentials, this.backupEnvironmentAuthentication.SubscriptionId);
        }

        internal async Task<IResourceGroup> CreateBackupResourceGroupIfDoesntExist(string resourceGroupName)
        {
            var exists = await this.targetStorageManager.ResourceManager.ResourceGroups.ContainAsync(resourceGroupName);

            if (!exists)
            {
                return await this.targetStorageManager.ResourceManager.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(this.setup.BackupTargetRegion).CreateAsync();
            }
            else
            {
                return await this.targetStorageManager.ResourceManager.ResourceGroups.GetByNameAsync(resourceGroupName);
            }
        }

        /// <summary>
        /// Perform a backup according to the given setup.
        /// </summary>
        /// <param name="tableBackupSetup"></param>
        /// <returns></returns>
        internal async Task<BackupResult> BackupTableAsync(TableBackupSetup tableBackupSetup, DateTime backupStartTime)
        {
            // Check when the table was last backed up.
            var lastBackupEntity = this.GetLastBackupEntity(tableBackupSetup);

            DateTime startTime = DateTime.UtcNow;

            IStorageAccount targetAccount;

            string sourceQuery = null;

            BackupReturnCode returnCode;

            if (lastBackupEntity == null)
            {
                // We've never backed this table up before.
                targetAccount = await this.GetNewAccount(backupStartTime);
                returnCode = BackupReturnCode.BackupToNewContainerDone;
            }
            else
            {
                // If the last backup started more than the configured frequency ago, we should run the backup now.
                var mustRunBackupNow = startTime - lastBackupEntity.BackupStartedAt >
                                       tableBackupSetup.IncrementalChangesFrequency;

                if (!mustRunBackupNow)
                {
                    return new BackupResult()
                    {
                        Setup = tableBackupSetup,
                        ReturnCode = BackupReturnCode.Nop,
                    };
                }

                DateTime timeWhenStorageContainerWasCreated =
                    this.GetDateFromStorageAccountName(lastBackupEntity.TargetStorageAccountName);

                // If the last backup is so long ago that we should change storage account target for the table, create a new storage account.
                var mustChangeTarget = startTime - timeWhenStorageContainerWasCreated > tableBackupSetup.SwitchTargetFrequency;

                if (mustChangeTarget)
                {
                    targetAccount = await this.GetNewAccount(DateTime.UtcNow);
                    returnCode = BackupReturnCode.BackupToNewContainerDone;
                }
                else
                {
                    targetAccount = await this.GetStorageAccount(lastBackupEntity.TargetStorageAccountName);

                    if (tableBackupSetup.BackupMode == BackupMode.Incremental)
                    {
                        sourceQuery = $"Timestamp gt datetime'{lastBackupEntity.BackupStartedAt.ToIso8601()}'";
                    }

                    returnCode = BackupReturnCode.BackupToExistingContainerDone;
                }
            }

            // Get the authentication info to connect to the target storage account.
            var storageKeys = await targetAccount.GetKeysAsync();
            var key = storageKeys.First().Value;
            var accountName = targetAccount.Name;

            IAzureTableStorage targetTableStorage = new AzureTableStorage(tableBackupSetup.SourceStorage.Name, this.GetStorageConnectionString(accountName, key));

            this.RunPipeline(tableBackupSetup, sourceQuery, targetTableStorage, startTime);

            DateTime end = DateTime.UtcNow;

            // Save an entity in the management table to remember the last time we ran.
            BackupManagementEntity historyEntity = new BackupManagementEntity(Guid.NewGuid().ToString(), tableBackupSetup.SourceStorage.Name, targetAccount.Name, targetTableStorage.Name, DateTime.UtcNow, startTime, end, BackupStatus.Success, tableBackupSetup.BackupMode);

            this.backupManagementTable.Insert(historyEntity);

            return new BackupResult()
            {
                Setup = tableBackupSetup,
                ReturnCode = returnCode,
                BackUpResourceGroup = this.setup.BackupTargetResourceGroupName,
                BackUpStorageAccountName = targetAccount.Name,
                BackUpTableName = targetTableStorage.Name
            };
        }

        internal void RunPipeline(TableBackupSetup backupSetup, string sourceQuery, IAzureTableStorage targetTableStorage, DateTimeOffset startTime)
        {
            CopySetup copySetup = new CopySetup
            {
                DeleteDataFactoryIfExists = true,
                TargetDatasetName = $"{targetTableStorage.Name}-target-dataset",
                SourceDatasetName = $"{targetTableStorage.Name}-source-dataset",
                CopyPipelineName = $"Copy-{targetTableStorage.Name}-{startTime.Year}-{startTime.Month}-{startTime.Day}",
                CreateTargetIfNotExists = true,
                SourceLinkedServiceName = $"{targetTableStorage.Name}-source-service",
                TargetLinkedServiceName = $"{targetTableStorage.Name}-target-service",
                TimeoutInMinutes = backupSetup.TimeoutInMinutes
            };

            CopyDataPipeline copyPipeline =
                CopyDataPipeline.UsingDataFactorySettings(this.setup.DataFactorySetup, copySetup, this.backupEnvironmentAuthentication, Console.WriteLine);

            copyPipeline.From(backupSetup.SourceStorage).To(targetTableStorage).UsingSourceQuery(sourceQuery).Start();

            copyPipeline.Start();
            copyPipeline.CleanUp();
        }

        private BackupManagementEntity GetLastBackupEntity(TableBackupSetup tableBackupSetup)
        {
            return this.backupManagementTable
                .Query<BackupManagementEntity>(p => p.SourceTableName == tableBackupSetup.SourceStorage.Name)
                .OrderByDescending(p => p.BackupStartedAt)
                .FirstOrDefault();
        }

        internal async Task<IStorageAccount> GetNewAccount(DateTimeOffset date)
        {
            string targetAccountName = $"{date:yyyyMMddHHmmss}{this.setup.BackupStorageAccountSuffix}";

            return await this.CreateStorageAccountIfNotExists(targetAccountName);
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
            var unexpectedNameException = new UnexpectedStorageAccountNameException($"The account name {storageAccountName} does not follow the format [year][month][day][hour][minute][second][name].");

            int expectedLength = 14;
            if (storageAccountName.Length < expectedLength)
            {
                throw unexpectedNameException;
            }

            // Take the first 14 characters, i.e. the part related to date (yyyyMMddHHmmss)
            string name = storageAccountName.Substring(0, 14);

            
            int startIndexOfYear = 0;
            int startIndexOfMonth = 4;
            int startIndexOfDay = 6;
            int startIndexOfHour = 8;
            int startIndexOfMinute = 10;
            int startIndexOfSecond = 12;

            if (!int.TryParse(name.Substring(startIndexOfYear, 4), out var year))
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(name.Substring(startIndexOfMonth, 2), out var month))
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(name.Substring(startIndexOfDay, 2), out var day))
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(name.Substring(startIndexOfHour, 2), out var hour))
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(name.Substring(startIndexOfMinute, 2), out var minute))
            {
                throw unexpectedNameException;
            }

            if (!int.TryParse(name.Substring(startIndexOfSecond, 2), out var second))
            {
                throw unexpectedNameException;
            }

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        internal async Task<IStorageAccount> CreateStorageAccountIfNotExists(string targetAccountName)
        {
            var account = await this.GetStorageAccount(targetAccountName);

            if (account == null)
            {
                account = await this.targetStorageManager
                    .StorageAccounts
                    .Define(targetAccountName)
                    .WithRegion(this.setup.BackupTargetRegion)
                    .WithExistingResourceGroup(this.setup.BackupTargetResourceGroupName)
                    .WithOnlyHttpsTraffic()
                    .CreateAsync();
            }

            return account;
        }

        internal async Task<IStorageAccount> GetStorageAccount(string name)
        {
            return await this.targetStorageManager.StorageAccounts.GetByResourceGroupAsync(this.setup.BackupTargetResourceGroupName, name);
        }

        internal AzureCredentials GetCredentials()
        {
            return new AzureCredentials(
                new ServicePrincipalLoginInformation()
                {
                    ClientId = this.backupEnvironmentAuthentication.Credentials.ClientId,
                    ClientSecret = this.backupEnvironmentAuthentication.Credentials.ClientSecret
                },
                this.backupEnvironmentAuthentication.Credentials.TenantId,
                this.setup.AzureEnvironment);
        }
    }
}