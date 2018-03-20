namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        [Category("IntegrationTest")]
        [Test]
        public void InsertManagementEntity()
        {
            BackupManagementTable managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());

            BackupManagementEntity historyEntity = new BackupManagementEntity(Guid.NewGuid().ToString(), "test", "test", "test", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, BackupStatus.Success, BackupMode.Full);

            managementTable.Insert(historyEntity);

            managementTable.GetFirst<BackupManagementEntity>(historyEntity.PartitionKey).Should().NotBeNull();

            managementTable.Delete(historyEntity);
        }

        /// <summary>
        /// Tests running a full backup once and verifies that the correct status code is returned.
        /// </summary>
        /// <returns></returns>
        [Category("IntegrationTest"), Category("LongRunning"), Category("Backup")]
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
            BackupSchedule schedule = new BackupSchedule()
            {
                SwitchTargetStorageFrequency = TimeSpan.FromMinutes(inconsequentialValue),
                IncrementalLoadFrequency = TimeSpan.FromMinutes(inconsequentialValue),
                RetentionTimeSpan = TimeSpan.FromMinutes(inconsequentialValue)
            };

            BackupSetup setup = this.GetTestSetup(resourceGroupName, testTable, schedule);

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

        /// <summary>
        /// Tests that first running one backup and then an incremental one, returns the right status code, indicating that an incremental
        /// load was performed.
        /// </summary>
        /// <returns></returns>
        [Category("IntegrationTest"), Category("LongRunning"), Category("Backup")]
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

            BackupSchedule schedule = new BackupSchedule()
            {
                IncrementalLoadFrequency = TimeSpan.FromMinutes(5),
                SwitchTargetStorageFrequency = TimeSpan.FromMinutes(someVeryHighValue),
                RetentionTimeSpan = TimeSpan.FromMinutes(someVeryHighValue)
            };

            // Get a backup setup that backs up the data in testtable.
            var setup = this.GetTestSetup(resourceGroupName, testTable, schedule);

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

        /// <summary>
        /// Tests that performing a backup with a single table where the backup schedule specifies that the backup expires right away,
        /// makes the clean up function delete the entire resource group that was created for the backup (since the table backup has expired,
        /// and there are no more backup tables left in the storage account).
        /// </summary>
        /// <returns></returns>
        [Category("IntegrationTest"), Category("LongRunning"), Category("Backup")]
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

            BackupSchedule schedule = new BackupSchedule()
            {
                IncrementalLoadFrequency = TimeSpan.FromMinutes(someVeryHighValue),
                SwitchTargetStorageFrequency = TimeSpan.FromMinutes(someVeryHighValue),
                RetentionTimeSpan = TimeSpan.FromMinutes(1)
            };

            // Get a backup setup that backs up the data in testtable.
            var setup = this.GetTestSetup(resourceGroupName, testTable, schedule);

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

        /// <summary>
        /// Tests that running two backups where one of them expires shortly and then invoking the clean up function, deletes one of the tables
        /// that were backed up, and not the other.
        /// </summary>
        /// <returns></returns>
        [Category("IntegrationTest"), Category("LongRunning"), Category("Backup")]
        [Test]
        public async Task CleanUpOutdatedBackups_NotAllTablesExpired()
        {

            string resourceGroupName = "testBackupResources4";
            BackupManagementTable managementTable = this.GetBackupManagementTable();

            try
            {
                managementTable.DeleteIfExists(true);

                // Create the backup.
                TableStorageBackup backup =
                    this.CreateTwoTableBackupWhereOneTableExpiresShortly(resourceGroupName, managementTable);

                var backupResults = (await backup.RunAsync()).ToList();

                // The backup has now been executed. To be absolutely sure, we wait a little while,
                // so that at least one minute has passed (the retention time of the backup)
                Thread.Sleep(1000 * 60);

                // ASSERT that the backup container contains both tables before the clean up.
                var backupResult = backupResults[0];
                var secondBackupResult = backupResults[1];
                var tablesBeforeCleanup =
                    (await this.ListBackupStorageAccountTablesAsync(backup, backupResult)).ToList();
                tablesBeforeCleanup.Select(p => p.Name).Should().Contain(backupResult.BackUpTableName,
                    "because the table should have been backed up now");
                tablesBeforeCleanup.Select(p => p.Name).Should().Contain(secondBackupResult.BackUpTableName,
                    "because the table should have been backed up now");

                // Invoke the clean up method, which should delete the only one of the tables we created during the
                // backup since it has expired.
                await backup.CleanUpOldBackupsAsync();

                // ASSERT that the storage account itself still exists, since there's only one of the two backed up tables
                // that should have expired. 
                var storageAccountsAfterCleanup = await this.ListStorageAccountsInResourceGroup(backup, backupResult);
                storageAccountsAfterCleanup.Select(p => p.Name).Should().Contain(backupResult.BackUpStorageAccountName,
                    "because only one of the two tables should have expired, and we should therefore not delete the storage account");

                // ASSERT that there is now one table that was cleaned up and one that was not.
                var tablesAfterCleanup =
                    (await this.ListBackupStorageAccountTablesAsync(backup, backupResult)).ToList();
                tablesAfterCleanup.Select(p => p.Name).Should().NotContain(backupResult.BackUpTableName,
                    "Because we set the expiry to a very short time, so the table should have been deleted.");
                tablesAfterCleanup.Select(p => p.Name).Should().Contain(secondBackupResult.BackUpTableName,
                    "because we set the expiry to a very long time, so it should still exist.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception in CleanUpOutdatedBackups_NotAllTablesExpired: {ex}");
                throw;
            }
            finally
            {
                IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();
                AzureCredentials credentials = new AzureCredentials(new ServicePrincipalLoginInformation()
                {
                    ClientId = auth.Credentials.ClientId,
                    ClientSecret = auth.Credentials.ClientSecret
                }, 
                this.environment.Credentials.TenantId, AzureEnvironment.AzureGlobalCloud);

                // Clean up...
                await this.DeleteResourceGroupAsync(resourceGroupName, credentials);
                managementTable.DeleteIfExists();
            }
        }

        /// <summary>
        /// Setup up a backup where there are two tables to back up, where one of them expires very shortly and the other one not at all.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="managementTable"></param>
        /// <returns></returns>
        internal TableStorageBackup CreateTwoTableBackupWhereOneTableExpiresShortly(string resourceGroupName, BackupManagementTable managementTable)
        {
            int someInconsequentialNumberOfEntities = 100;
            IAzureTableStorage testTable = this.CreateAndPopulateTestTable("TestTable", someInconsequentialNumberOfEntities);
            IAzureTableStorage testTable2 =
                this.CreateAndPopulateTestTable("TestTable2", someInconsequentialNumberOfEntities);

            // Set the retention time very low, so that when we ask for a clean up, we can expect the
            // container to be deleted.
            var backupWillNotExpire = this.GetBackupScheduleThatExpiresIn(TimeSpan.MaxValue);
            var backupExpiresInOneMinute = this.GetBackupScheduleThatExpiresIn(TimeSpan.FromMinutes(1));

            // Get two setups, one that backs testTable up, but where the backup expires in one minute and one that backs testtable2 up,
            // but that will never expire.
            var setup = this.GetTestSetup(resourceGroupName, testTable, backupExpiresInOneMinute);
            var setup2 = this.GetTestSetup(resourceGroupName, testTable2, backupWillNotExpire);
            setup.TablesToBackup.Add(setup2.TablesToBackup.First());

            // Setup now contains two table backups, where one of them will expire shortly (one minute).

            IAzureActiveDirectoryAuthentication auth = this.GetAuthentication();

            // Create the backup.
            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);

            return backup;

        }

        internal IAzureTableStorage CreateAndPopulateTestTable(string name, int numberOfEntities)
        {
            IAzureTableStorage testTable = this.GetTestTable(name);

            testTable.DeleteIfExists();

            var testEntities = TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);

            return testTable;
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

        private BackupSetup GetTestSetup(string resourceGroupName, IAzureTableStorage sourceTable, BackupSchedule schedule)
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
                        TimeoutInMinutes = 5,
                        BackupMode = BackupMode.Full,
                        Schedule = schedule
                    }
                }
            };
        }

        private BackupSchedule GetBackupScheduleThatExpiresIn(TimeSpan span)
        {
            int someVeryHighValue = 1000;
            var longTimeSpan = TimeSpan.FromDays(someVeryHighValue);

            return new BackupSchedule()
            {
                SwitchTargetStorageFrequency = longTimeSpan,
                IncrementalLoadFrequency = longTimeSpan,
                RetentionTimeSpan = span
            };
        }

        private BackupManagementTable GetBackupManagementTable()
        {
            var managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());
            managementTable.DeleteIfExists(true);

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
