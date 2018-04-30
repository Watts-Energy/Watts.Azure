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

        private readonly Action<string> progressDelegate;

        /// <summary>
        /// Create a new instance of TableStorageBackup.
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="backupEnvironmentAuthentication"></param>
        /// <param name="backupManagementTable"></param>
        /// <param name="reportProgressOn"></param>
        public TableStorageBackup(BackupSetup setup, IAzureActiveDirectoryAuthentication backupEnvironmentAuthentication, BackupManagementTable backupManagementTable, Action<string> reportProgressOn = null)
        {
            this.setup = setup;
            this.backupEnvironmentAuthentication = backupEnvironmentAuthentication;
            this.backupManagementTable = backupManagementTable;
            this.progressDelegate = reportProgressOn;

            this.Initialize();
        }

        /// <summary>
        /// Run the backup.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<BackupResult>> RunAsync(bool parallelize = false)
        {
            this.Report("Backup starting");

            var retVal = new List<BackupResult>();

            DateTime backupStartTime = DateTime.UtcNow;

            // Create the resource group if it doesn't already exist.
            await this.CreateBackupResourceGroupIfDoesntExist(this.setup.BackupTargetResourceGroupName);

            // Go through each table and perform the backup.
            if (parallelize)
            {
                Parallel.ForEach(this.setup.TablesToBackup, async tableBackupSetup =>
                {
                    retVal.Add(await this.BackupTableAsync(tableBackupSetup, backupStartTime));
                });
            }
            else
            {
                foreach (var backup in this.setup.TablesToBackup)
                {
                    var backupResult = await this.BackupTableAsync(backup, backupStartTime);
                    retVal.Add(backupResult);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Clean up any expired backups, including the resource groups in which they reside, provided the resource group no longer
        /// contains table backups.
        /// </summary>
        /// <returns>A list of names of the storage accounts that were deleted.</returns>
        public async Task<IEnumerable<string>> CleanUpOldBackupsAsync()
        {
            this.Report($"Cleaning old backups...");

            List<string> retVal = new List<string>();

            // Get all storage accounts in the target resource group.
            var accounts =
                await this.targetStorageManager.StorageAccounts.ListByResourceGroupAsync(this.setup.BackupTargetResourceGroupName, true);

            DateTime now = DateTime.UtcNow;

            this.Report($"Checking for expired backups...");

            // Check all accounts for tables that have expired. If the storage account no longer contains any tables, it is also deleted...
            foreach (var account in accounts)
            {
                this.Report($"Checking account {account.Name}");

                List<string> tablesToDelete = new List<string>();

                var createdDate = this.GetDateFromStorageAccountName(account.Name);

                // Check each backup setup we have to see if there's anything that
                // should be deleted in the resource group (expired backups).
                foreach (var tableBackup in this.setup.TablesToBackup)
                {
                    // If the retentiontime has passed, add the table name for deletion from the backup
                    // storage account
                    if (now - createdDate > tableBackup.Schedule.RetentionTimeSpan)
                    {
                        this.Report($"{tableBackup.SourceStorage.Name} has expired in storage account {account.Name}...");

                        // We should delete the table
                        tablesToDelete.Add(tableBackup.SourceStorage.Name);
                    }
                }

                if (tablesToDelete.Any())
                {
                    // Delete any tables that have expired.
                    await this.DeleteTablesAsync(account, tablesToDelete);
                }
                else
                {
                    this.Report("No tables to delete this.time...");
                }

                // Delete the storage account if there are no more tables in it.
                var deleted = await this.DeleteStorageAccountIfNoMoreTablesAsync(account);

                if (deleted)
                {
                    retVal.Add(account.Name);
                }

                this.Report($"{account.Name} cleaned...");
            }

            return retVal;
        }

        /// <summary>
        /// Initialize the backup by settings credentials and creating the storage manager used manage resources.
        /// </summary>
        internal void Initialize()
        {
            this.Report("Initializing...");
            var backupEnvironmentCredentials = this.GetCredentials();

            this.targetStorageManager = StorageManager.Authenticate(backupEnvironmentCredentials, this.backupEnvironmentAuthentication.SubscriptionId);
        }

        internal async Task<bool> DeleteStorageAccountIfNoMoreTablesAsync(IStorageAccount account)
        {
            CloudStorageAccount tableAccount = CloudStorageAccount.Parse(this.GetStorageConnectionString(account.Name, (await account.GetKeysAsync()).First().Value));
            CloudTableClient tableClient = tableAccount.CreateCloudTableClient();

            if (!tableClient.ListTables().Any())
            {
                this.Report($"The storage account {account.Name} has no more tables. Will delete it now...");
                await this.targetStorageManager.StorageAccounts.DeleteByIdAsync(account.Id);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Delete the named tables in the storage account.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        internal async Task DeleteTablesAsync(IStorageAccount account, List<string> tableNames)
        {
            this.Report($"Deleting table(s) {string.Join(",", tableNames)} from storage account {account.Name}");

            string currentTableName = string.Empty;
            try
            {
                // Get the keys from the storage account and construct a connection string.
                var keys = await account.GetKeysAsync();
                var connectionString = this.GetStorageConnectionString(account.Name, keys.First().Value);

                // Iterate tables and delete them.
                foreach (var tableName in tableNames)
                {
                    currentTableName = tableName;

                    IAzureTableStorage tableStorage = new AzureTableStorage(tableName, connectionString);

                    // If the table does not exist, just skip.
                    if (!await tableStorage.ExistsAsync())
                    {
                        this.Report($"Table {tableName} does not exist in storage account {account.Name}");
                    }
                    else
                    {
                        var success = tableStorage.DeleteIfExists();

                        if (!success)
                        {
                            this.Report($"There was a problem deleting the table {tableName}");
                            throw new TableDeleteFailedException(tableName);
                        }

                        this.Report($"{tableName} deleted...");
                    }
                }
            }
            catch (Exception)
            {
                throw new TableDeleteFailedException(currentTableName);
            }
        }

        /// <summary>
        /// Create the resource group if it doesnt exist. 
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <returns>A reference to the resource group that either already existed, or was just created.</returns>
        internal async Task<IResourceGroup> CreateBackupResourceGroupIfDoesntExist(string resourceGroupName)
        {
            var exists = await this.targetStorageManager.ResourceManager.ResourceGroups.ContainAsync(resourceGroupName);

            if (!exists)
            {
                this.Report($"Resource group {resourceGroupName} did not exist. Will create it...");
                return await this.targetStorageManager.ResourceManager.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(this.setup.BackupTargetRegion).CreateAsync();
            }
            else
            {
                this.Report($"Resource group {resourceGroupName} already exists...");
                return await this.targetStorageManager.ResourceManager.ResourceGroups.GetByNameAsync(resourceGroupName);
            }
        }

        /// <summary>
        /// Perform a backup according to the given setup.
        /// </summary>
        /// <param name="tableBackupSetup">Setup for the table backup.</param>
        /// <param name="backupStartTime">The time the backup started, needed to group backups into storage accounts.</param>
        /// <returns></returns>
        internal async Task<BackupResult> BackupTableAsync(TableBackupSetup tableBackupSetup, DateTime backupStartTime)
        {
            this.Report($"Backing up table {tableBackupSetup.SourceStorage.Name}");

            // Check when the table was last backed up.
            var lastBackupEntity = this.GetLastBackupEntity(tableBackupSetup);

            DateTime startTime = DateTime.UtcNow;

            IStorageAccount targetAccount;

            string sourceQuery = null;

            BackupReturnCode returnCode;

            // If we've never backed this table up before, perform a full table backup.
            if (lastBackupEntity == null)
            {
                this.Report($"First backup of table {tableBackupSetup.SourceStorage.Name}");

                // We've never backed this table up before.
                targetAccount = await this.GetNewAccount(backupStartTime);
                returnCode = BackupReturnCode.BackupToNewContainerDone;
            }
            else
            {
                // If there is a backup in progress, just return
                if (lastBackupEntity.Status == (int)BackupStatus.InProgress)
                {
                    this.Report($"A backup of {tableBackupSetup.SourceStorage.Name} is already in progress...");

                    return new BackupResult()
                    {
                        Setup = tableBackupSetup,
                        ReturnCode = BackupReturnCode.Nop,
                        BackUpTableName = tableBackupSetup.SourceStorage.Name,
                    };
                }

                bool mustRunBackupNow = this.ShouldRunBackupNow(startTime, lastBackupEntity, tableBackupSetup);

                // If we don't need to run the backup now, just return a result indicating a no-operation.
                if (!mustRunBackupNow)
                {
                    this.Report($"No need to back up yet, as the time since the last backup ({startTime - lastBackupEntity.BackupStartedAt}) is less than both the incremental load frequency ({tableBackupSetup.Schedule.IncrementalLoadFrequency}) and switch target frequency ({tableBackupSetup.Schedule.SwitchTargetStorageFrequency})");

                    return new BackupResult()
                    {
                        Setup = tableBackupSetup,
                        ReturnCode = BackupReturnCode.Nop,
                        BackUpTableName = tableBackupSetup.SourceStorage.Name
                    };
                }

                // Get the time when the storage account was created, to check whether we need to switch to a new storage account for this table.
                DateTime timeWhenStorageContainerWasCreated =
                    this.GetDateFromStorageAccountName(lastBackupEntity.TargetStorageAccountName);

                // If the last backup is so long ago that we should change storage account target for the table, create a new storage account.
                var mustChangeTarget = startTime - timeWhenStorageContainerWasCreated > tableBackupSetup.Schedule.SwitchTargetStorageFrequency;

                if (mustChangeTarget)
                {
                    targetAccount = await this.GetNewAccount(DateTime.UtcNow);

                    this.Report($"Time to switch to a new storage account. Created {targetAccount}");
                    returnCode = BackupReturnCode.BackupToNewContainerDone;
                }
                else
                {
                    this.Report($"Will target existing storage account {lastBackupEntity.TargetStorageAccountName}");

                    // Get the account that this table was backed up to last.
                    targetAccount = await this.GetStorageAccount(lastBackupEntity.TargetStorageAccountName);

                    // If it's an incremental backup, construct a query that will filter out any entities that have already been backed up.
                    if (tableBackupSetup.BackupMode == BackupMode.Incremental)
                    {
                        sourceQuery = $"Timestamp gt datetime'{lastBackupEntity.BackupStartedAt.ToIso8601()}'";
                        this.Report($"Incremental backup. Source query will be {sourceQuery}");
                    }

                    returnCode = BackupReturnCode.BackupToExistingContainerDone;
                }
            }

            // Get the authentication info to connect to the target storage account.
            var storageKeys = await targetAccount.GetKeysAsync();
            var key = storageKeys.First().Value;
            var accountName = targetAccount.Name;

            IAzureTableStorage targetTableStorage = new AzureTableStorage(tableBackupSetup.SourceStorage.Name, this.GetStorageConnectionString(accountName, key));

            // Insert an entity indicating that a backup is in progress...
            BackupManagementEntity managementEntity = new BackupManagementEntity(Guid.NewGuid().ToString(), tableBackupSetup.SourceStorage.Name, targetAccount.Name, tableBackupSetup.SourceStorage.Name, DateTime.UtcNow, startTime, null, BackupStatus.InProgress, tableBackupSetup.BackupMode);
            this.backupManagementTable.Insert(managementEntity);

            var status = BackupStatus.Success;
            try
            {
                this.RunPipeline(tableBackupSetup, sourceQuery, targetTableStorage, startTime);
            }
            catch (Exception ex)
            {
                status = BackupStatus.Failure;
            }
            finally
            {
                // Update the management entity to indicate that the backup has now finished
                managementEntity.BackupFinishedAt = DateTimeOffset.UtcNow;
                managementEntity.Status = (int)status;
                this.backupManagementTable.Update(managementEntity);
            }

            return new BackupResult()
            {
                Setup = tableBackupSetup,
                ReturnCode = returnCode,
                BackUpResourceGroup = this.setup.BackupTargetResourceGroupName,
                BackUpStorageAccountName = targetAccount.Name,
                BackUpTableName = targetTableStorage.Name
            };
        }

        internal bool ShouldRunBackupNow(DateTime startTime, BackupManagementEntity lastBackupEntity, TableBackupSetup tableBackupSetup)
        {
            // If the time since the last backup exceeds either the incremental load frequency or the switch target frequency, we must run a backup.
            var incrementalLoadFrequencyExceeded = startTime - lastBackupEntity.BackupStartedAt >
                                                   tableBackupSetup.Schedule.IncrementalLoadFrequency;
            var switchTargetFrequencyExceeded = startTime - lastBackupEntity.BackupStartedAt >
                                                tableBackupSetup.Schedule.SwitchTargetStorageFrequency;
            var mustRunBackupNow = incrementalLoadFrequencyExceeded || switchTargetFrequencyExceeded;

            return mustRunBackupNow;
        }

        internal void RunPipeline(TableBackupSetup backupSetup, string sourceQuery, IAzureTableStorage targetTableStorage, DateTimeOffset startTime)
        {
            this.Report($"Running pipeline to back up {backupSetup.SourceStorage.Name}...");

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
                CopyDataPipeline.UsingDataFactorySettings(this.setup.DataFactorySetup, copySetup, this.backupEnvironmentAuthentication, progress => this.Report($"Pipeline: {progress}"));
            copyPipeline.From(backupSetup.SourceStorage).To(targetTableStorage).UsingSourceQuery(sourceQuery).Start();

            copyPipeline.Start();

            this.Report($"Cleaning up pipeline");
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

            this.Report($"Getting or creating storage account {targetAccountName}");
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

            int TryParse(string input)
            {
                if (!int.TryParse(input, out var outValue))
                {
                    throw unexpectedNameException;
                }

                return outValue;
            }

            var year = TryParse(name.Substring(startIndexOfYear, 4));
            var month = TryParse(name.Substring(startIndexOfMonth, 2));
            var day = TryParse(name.Substring(startIndexOfDay, 2));
            var hour = TryParse(name.Substring(startIndexOfHour, 2));
            var minute = TryParse(name.Substring(startIndexOfMinute, 2));
            var second = TryParse(name.Substring(startIndexOfSecond, 2));

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

        internal void Report(string progress)
        {
            this.progressDelegate?.Invoke($"{DateTime.Now}: {progress}");
        }
    }
}