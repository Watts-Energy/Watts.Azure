namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Common.Backup;
    using Common.DataFactory.General;
    using Common.Interfaces.Security;
    using Common.Interfaces.Storage;
    using Common.Security;
    using Common.Storage.Objects;
    using FluentAssertions;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.Storage.Fluent;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;
    using Objects;
    using Utils.Objects;
    using Constants = Tests.Constants;

    [TestFixture]
    public class BackupIntegrationTests
    {
        private TestEnvironmentConfig config;

        private DataCopyEnvironment environment;

        [SetUp]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.environment = this.config.DataCopyEnvironment;
        }

        [Test]
        public void InsertManagementEntity()
        {
            BackupManagementTable managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());

            BackupManagementEntity historyEntity = new BackupManagementEntity(Guid.NewGuid().ToString(), "test", "test", "test", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, BackupStatus.Success, BackupMode.Full);

            managementTable.Insert(historyEntity);

            managementTable.GetFirst<BackupManagementEntity>(historyEntity.PartitionKey).Should().NotBeNull();

            managementTable.Delete(historyEntity);
        }

        [Category("IntegrationTest")]
        [Test]
        public async Task RunFullBackupOnce()
        {
            string resourceGroupName = "testBackupResources";

            AzureTableStorage testTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "TestBackup");

            BackupManagementTable managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());

            var testEntities = TestFacade.RandomEntities(200, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);

            // Set the time spans to some value. It doesn't really matter, as we're just running one full
            // backup of a table that doesn't already exist.
            int inconsequentialValue = 100;
            var switchToNewContainerSpan = TimeSpan.FromMinutes(inconsequentialValue);
            var incrementalLoadSpan = TimeSpan.FromMinutes(inconsequentialValue);
            var retentionTime = TimeSpan.FromMinutes(inconsequentialValue);

            BackupSetup setup = this.GetTestSetup(resourceGroupName, testTable, switchToNewContainerSpan,
                incrementalLoadSpan, retentionTime);

            IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();

            // Create the backup.
            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);

            var result = await backup.RunAsync();

            // Assert that we got full backups to a new container (since one didn't already exist)
            result.ToList().ForEach(p => p.ReturnCode.Should().Be(BackupReturnCode.BackupToNewContainerDone));

            // Clean up...
            await this.DeleteResourceGroupAsync(resourceGroupName, backup.GetCredentials());
            managementTable.DeleteIfExists();
        }

        [Category("IntegrationTest")]
        [Test]
        public async Task RunIncrementalBackup()
        {
            string resourceGroupName = "testBackupResources2";

            IAzureTableStorage testTable = this.GetTestTable();
            BackupManagementTable managementTable = this.GetBackupManagementTable();

            var testEntities = TestFacade.RandomEntities(100, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);

            // Set the incremental load frequency to 5 minutes and the retention time and 
            // time between switches to a new container very high, so that we know that if we run
            // two consecutive backups, we should get an incremental load.
            int someVeryHighValue = 1000;

            var switchTargetFrequency = TimeSpan.FromMinutes(someVeryHighValue);
            var incrementalLoadFrequency = TimeSpan.FromMinutes(5);
            var retentionTime = TimeSpan.FromMinutes(someVeryHighValue);

            // Get a backup setup that backs up the data in testtable.
            var setup = this.GetTestSetup(resourceGroupName, testTable, switchTargetFrequency, incrementalLoadFrequency, retentionTime);

            IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();

            // Create the backup.
            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);
            await backup.RunAsync();

            // The first backup has run. Now wait five minutes before running the next backup, and
            // assert that it was incremental.
            int millisecondsInFiveMinutes = 1000 * 60 * 5;
            Thread.Sleep(millisecondsInFiveMinutes);
            var result = await backup.RunAsync();

            result.First().ReturnCode.Should().Be(BackupReturnCode.BackupToExistingContainerDone);

            // Clean up...
            await this.DeleteResourceGroupAsync(resourceGroupName, backup.GetCredentials());
            managementTable.DeleteIfExists();
        }

        [Category("IntegrationTest")]
        [Test]
        public async Task CleanUpOutdatedBackups()
        {
            string resourceGroupName = "testBackupResources3";

            IAzureTableStorage testTable = this.GetTestTable();
            BackupManagementTable managementTable = this.GetBackupManagementTable();

            var testEntities = TestFacade.RandomEntities(100, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);

            // Set the retention time very low, so that when we ask for a clean up, we can expect the
            // container to be deleted.
            int someVeryHighValue = 1000;

            var switchTargetFrequency = TimeSpan.FromMinutes(someVeryHighValue);
            var incrementalLoadFrequency = TimeSpan.FromMinutes(someVeryHighValue);
            var retentionTime = TimeSpan.FromMinutes(1);

            // Get a backup setup that backs up the data in testtable.
            var setup = this.GetTestSetup(resourceGroupName, testTable, switchTargetFrequency, incrementalLoadFrequency, retentionTime);

            IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();

            // Create the backup.
            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);
            var backupResults = await backup.RunAsync();

            // The backup has now been executed. To be absolutely sure, we wait a little while,
            // so that at least one minute has passed (the retention time)
            Thread.Sleep(1000 * 60);
            var backupResult = backupResults.First();
            var storageAccountsBeforeCleanup = await this.ListStorageAccountsInResourceGroup(backup, backupResult);

            // Check that the resource group now contains the storage account created by the backup.
            storageAccountsBeforeCleanup.Select(p => p.Name).Should().Contain(backupResult.BackUpStorageAccountName);

            // Invoke the clean up method, which should delete the table we created during the
            // backup since it has expired.
            await backup.CleanUpOldBackupsAsync();
            
            // ASSERT that since the testtable was the only table in the storage account,
            // the storage account has been deleted.
            var storageAccountsAfterCleanup = await this.ListStorageAccountsInResourceGroup(backup, backupResult);
            storageAccountsAfterCleanup.Select(p => p.Name).Should().NotContain(backupResult.BackUpStorageAccountName);

            // Clean up...
            await this.DeleteResourceGroupAsync(resourceGroupName, backup.GetCredentials());
            managementTable.DeleteIfExists();
        }

        [Category("IntegrationTest")]
        [Test]
        public async Task CleanUpOutdatedBackups_NotAllTablesExpired()
        {
           string resourceGroupName = "testBackupResources4";

            IAzureTableStorage testTable = this.GetTestTable();
            IAzureTableStorage testTable2 = this.GetTestTable("TestTable2");

            testTable.DeleteIfExists();
            testTable2.DeleteIfExists();

            Thread.Sleep(60000);

            BackupManagementTable managementTable = this.GetBackupManagementTable();
            managementTable.DeleteIfExists();

            var testEntities = TestFacade.RandomEntities(100, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);
            testTable2.Insert(testEntities, null);

            // Set the retention time very low, so that when we ask for a clean up, we can expect the
            // container to be deleted.
            int someVeryHighValue = 1000;

            var switchTargetFrequency = TimeSpan.FromMinutes(someVeryHighValue);
            var incrementalLoadFrequency = TimeSpan.FromMinutes(someVeryHighValue);
            var retentionTime = TimeSpan.FromMinutes(1);

            // Get a backup setup that backs up the data in testtable.
            var setup = this.GetTestSetup(resourceGroupName, testTable, switchTargetFrequency, incrementalLoadFrequency, retentionTime);

            var veryLargeTimeSpan = TimeSpan.MaxValue;
            var setup2 = this.GetTestSetup(resourceGroupName, testTable2, veryLargeTimeSpan, veryLargeTimeSpan,
                veryLargeTimeSpan);

            // Add the table backup from setup2, which is a backup that will not expire, ever.
            setup.TablesToBackup.Add(setup2.TablesToBackup.First());

            IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();

            // Create the backup.
            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);
            var backupResults = (await backup.RunAsync()).ToList();

            // The backup has now been executed. To be absolutely sure, we wait a little while,
            // so that at least one minute has passed (the retention time)
            Thread.Sleep(1000 * 60);
            var backupResult = backupResults[0];
            var secondBackupResult = backupResults[1];
            var tablesBeforeCleanup = (await this.ListBackupStorageAccountTablesAsync(backup, backupResult)).ToList();

            // Check that the backup container contains both the tables
            tablesBeforeCleanup.Select(p => p.Name).Should().Contain(backupResult.BackUpTableName, "because the table should have been backed up now");
            tablesBeforeCleanup.Select(p => p.Name).Should().Contain(secondBackupResult.BackUpTableName, "because the table should have been backed up now");

            // Invoke the clean up method, which should delete the only one of the tables we created during the
            // backup since it has expired.
            await backup.CleanUpOldBackupsAsync();

            // ASSERT that the storage account itself still exists, since there's only one table
            // that should have expired. 
            var storageAccountsAfterCleanup = await this.ListStorageAccountsInResourceGroup(backup, backupResult);
            storageAccountsAfterCleanup.Select(p => p.Name).Should().Contain(backupResult.BackUpStorageAccountName, "because only one of the two tables should have expired, we should not delete the storage account");

            var tablesAfterCleanup = (await this.ListBackupStorageAccountTablesAsync(backup, backupResult)).ToList();

            tablesAfterCleanup.Select(p => p.Name).Should().NotContain(backupResult.BackUpTableName, "Because we set the expiry to a very short time, so the table should have been deleted.");
            tablesAfterCleanup.Select(p => p.Name).Should().Contain(secondBackupResult.BackUpTableName, "because we set the expiry to a very long time, so it should still exist.");

            // Clean up...
            await this.DeleteResourceGroupAsync(resourceGroupName, backup.GetCredentials());
            managementTable.DeleteIfExists();
        }

        private async Task<IEnumerable<IStorageAccount>> ListStorageAccountsInResourceGroup(TableStorageBackup backup,
            BackupResult backupResult)
        {
            var backupEnvironmentCredentials = backup.GetCredentials();

            var storageManager = StorageManager.Authenticate(backupEnvironmentCredentials, this.GetAuthentication().SubscriptionId);

            var storageAccount =
                await storageManager.StorageAccounts.ListByResourceGroupAsync(backupResult.BackUpResourceGroup, true);

            return storageAccount.ToList();
        }

        private async Task<IEnumerable<CloudTable>> ListBackupStorageAccountTablesAsync(TableStorageBackup backup, BackupResult backupResult)
        {
            var backupEnvironmentCredentials = backup.GetCredentials();

            var storageManager = StorageManager.Authenticate(backupEnvironmentCredentials, this.GetAuthentication().SubscriptionId);

            var storageAccount =
                await storageManager.StorageAccounts.GetByResourceGroupAsync(backupResult.BackUpResourceGroup,
                    backupResult.BackUpStorageAccountName);

            CloudStorageAccount tableAccount = CloudStorageAccount.Parse(backup.GetStorageConnectionString(storageAccount.Name, (await storageAccount.GetKeysAsync()).First().Value));
            CloudTableClient tableClient = tableAccount.CreateCloudTableClient();

            return tableClient.ListTables();
        }

        private BackupSetup GetTestSetup(string resourceGroupName, IAzureTableStorage sourceTable, TimeSpan switchTargetsSpan, TimeSpan incrementalLoadFrequency, TimeSpan retentionTime)
        {
           return new BackupSetup()
            {
                AzureEnvironment = AzureEnvironment.AzureGlobalCloud,
                BackupTargetRegion = Region.USEast,
                BackupTargetResourceGroupName = resourceGroupName,
                DataFactorySetup = this.environment.DataFactorySetup,
                BackupStorageAccountSuffix = Guid.NewGuid().ToString("N").Substring(0, 10),
                TablesToBackup = new List<TableBackupSetup>()
                {
                    new TableBackupSetup()
                    {
                        SourceStorage = sourceTable,
                        SwitchTargetFrequency = switchTargetsSpan,
                        TimeoutInMinutes = 5,
                        BackupMode = BackupMode.Full,
                        IncrementalChangesFrequency = incrementalLoadFrequency,
                        RetentionTime = retentionTime
                    }
                }
            };
        }

        private BackupManagementTable GetBackupManagementTable()
        {
            var managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());
            managementTable.DeleteIfExists();

            return managementTable;
        }

        private async Task DeleteResourceGroupAsync(string resourceGroupName, AzureCredentials credentials)
        {
            await StorageManager
                .Authenticate(credentials, this.environment.SubscriptionId)
                .ResourceManager
                .ResourceGroups
                .DeleteByNameAsync(resourceGroupName);
        }

        private IAzureTableStorage GetTestTable(string name = "TestTable")
        {
            return AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), name);
        }

        private IAzureActiveDirectoryAuthentication GetAuthentication()
        {
            return new AzureActiveDirectoryAuthentication(this.environment.SubscriptionId, this.environment.DataFactorySetup.ResourceGroupName, this.environment.Credentials);
        }
    }
}
