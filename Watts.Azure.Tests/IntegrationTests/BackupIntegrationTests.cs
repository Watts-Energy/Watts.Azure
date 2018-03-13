namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Common;
    using Common.Backup;
    using Common.DataFactory.General;
    using Common.Interfaces.Security;
    using Common.Security;
    using Common.Storage.Objects;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
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
        public void RunBackup()
        {
            AzureTableStorage testTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "TestBackup");

            BackupManagementTable managementTable = new BackupManagementTable(this.environment.GetDataFactoryStorageAccountConnectionString());

            var testEntities = TestFacade.RandomEntities(200, Guid.NewGuid().ToString()).ToList();

            testTable.Insert(testEntities, null);

            BackupSetup setup = new BackupSetup()
            {
                AzureEnvironment = AzureEnvironment.AzureGlobalCloud,
                BackupTargetRegion = Region.EuropeNorth,
                BackupTargetResourceGroupName = "testBackupResources",
                DataFactorySetup = this.environment.DataFactorySetup,
                BackupStorageAccountPrefix = Guid.NewGuid().ToString("N").Substring(0, 10),
                TablesToBackup = new  List<TableBackupSetup>()
                {
                    new TableBackupSetup()
                    {
                        SourceStorage = testTable,
                        SwitchTargetFrequency = TimeSpan.FromMinutes(10),
                        TimeoutInMinutes = 5,
                        BackupMode = BackupMode.Full,
                        IncrementalChangesFrequency = TimeSpan.FromMinutes(5),
                        RetentionTime = TimeSpan.FromMinutes(30)
                    }
                }
            };

            IAzureActiveDirectoryAuthentication auth = new AzureActiveDirectoryAuthentication(this.environment.SubscriptionId, this.environment.DataFactorySetup.ResourceGroupName, this.environment.Credentials);

            TableStorageBackup backup = new TableStorageBackup(setup, auth, managementTable);

            backup.Run().Wait();
        }
    }
}
