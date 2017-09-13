namespace Watts.Azure.Tests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common.Security;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Tests.Objects;
    using Watts.Azure.Utils.Build;
    using Watts.Azure.Utils.Objects;
    using Watts.Azure.Common.DataFactory.Copy;
    using System.Diagnostics;

    [TestClass]
    public class DataFactoryIntegrationTests
    {
        private PredefinedDataCopyEnvironment environment;
        private PredefinedDataLakeStoreEnvironment dataLakeEnvironment;
        private AzureActiveDirectoryAuthentication dataFactoryAuthentication;
        private AzureActiveDirectoryAuthentication dataLakeAuthentication;

        [TestInitialize]
        public void Setup()
        {
            this.environment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataCopyEnvironment;

            this.dataLakeEnvironment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataLakeEnvironment;

            this.dataFactoryAuthentication = new AzureActiveDirectoryAuthentication(
                this.environment.SubscriptionId,
                string.Empty,
                new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = this.environment.AdfClientId,
                    ClientSecret = this.environment.ClientSecret,
                    TenantId = this.environment.ActiveDirectoryTenantId
                });

            this.dataLakeAuthentication = new AzureActiveDirectoryAuthentication(
                this.dataLakeEnvironment.SubscriptionId,
                this.dataLakeEnvironment.ResourceGroupName,
                new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = this.dataLakeEnvironment.AdfClientId,
                    ClientSecret = this.dataLakeEnvironment.ClientSecret,
                    TenantId = this.dataLakeEnvironment.ActiveDirectoryTenantId
                });
        }

        [TestCategory("IntegrationTest"), TestCategory("DataFactory")]
        [TestMethod]
        public void CopyData_SimpleDataCopyTableToTable_Works()
        {
            // ARRANGE
            // Create the source and target tables
            AzureTableStorage sourceTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "SourceTableTest");
            AzureTableStorage targetTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "TargetTableTest");

            // Delete both tables if they exist and populate the source table with some data
            var deleted = sourceTable.DeleteIfExists();
            deleted &= targetTable.DeleteIfExists();

            if (deleted)
            {
                // Sleep a minute to ensure that Azure has actually deleted the table
                Thread.Sleep(60000);
            }

            int numberOfEntities = 10;

            sourceTable.Insert(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()));

            // Perform the copy.
            DataCopyBuilder
                .InDataFactoryEnvironment(this.environment)
                .UsingDataFactorySetup(this.environment.DataFactorySetup)
                .UsingDefaultCopySetup()
                .WithTimeoutInMinutes(20)
                .AuthenticateUsing(this.dataFactoryAuthentication)
                .CopyFrom(sourceTable)
                .WithSourceQuery(null)
                .To(targetTable)
                .ReportProgressToConsole()
                .StartCopy();

            // Assert that numberOfEntities are now present in the target table.
            var entitiesInTargetTable = targetTable.GetTop(1000);

            Assert.AreEqual(10, entitiesInTargetTable.Count);
        }

        /// <summary>
        /// Tests that copying data from a table to a data lake works. A table is populated with a number of entities, which are then copied to data lake.
        /// The resulting file in the data lake store is then downloaded and the number of lines compared with the number of entities inserted into the table.
        /// </summary>
        [TestCategory("IntegrationTest")]
        [TestCategory("DataFactory")]
        [TestMethod]
        public void CopySimpleData_FromTable_ToDataLake()
        {
            // ARRANGE
            AzureTableStorage sourceTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "SourceTableDataLakeTest");
            AzureDataLakeStore targetDataLake = new AzureDataLakeStore(this.dataLakeEnvironment.SubscriptionId, "/copydatatest", this.dataLakeEnvironment.DataLakeStoreName, this.dataLakeAuthentication);

            var deleted = sourceTable.DeleteIfExists();
            if (deleted)
            {
                // Sleep a minute to ensure that Azure has actually deleted the table
                Thread.Sleep(60000);
            }

             int numberOfEntities = 10;

            sourceTable.Insert(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()));

            CopySetup setup = new CopySetup()
            {
                CopyPipelineName = "IntegrationTestTableToDL",
                CreateTargetIfNotExists = true,
                SourceDatasetName = "TestDataSource",
                SourceLinkedServiceName = "TestTableStorage",
                TargetDatasetName = "TestDataTarget",
                TargetLinkedServiceName = "TestDataLake",
                TimeoutInMinutes = 20
            };

            // ACT
            DataCopyBuilder
                .InDataFactoryEnvironment(this.environment)
                .UsingDataFactorySetup(this.environment.DataFactorySetup)
                .UsingCopySetup(setup)
                .AuthenticateUsing(this.dataFactoryAuthentication)
                .CopyFrom(sourceTable)
                .To(targetDataLake)
                .ReportProgressTo((progress) => { Trace.WriteLine(progress); })
                .StartCopy();

            string downloadFileName = "./" + setup.TargetDatasetName + ".txt";

            targetDataLake.DownloadFile(string.Join("/", targetDataLake.Directory, setup.TargetDatasetName), downloadFileName, true);

            string[] contents = File.ReadAllLines(downloadFileName);

            // ASSERT that the number of lines are equal to one for each entity + one header.
            Assert.AreEqual(numberOfEntities + 1, contents.Length);

            // Clean up
            targetDataLake.DeleteDirectory(string.Empty).Wait();
            sourceTable.DeleteIfExists();
            File.Delete(downloadFileName);
        }
    }
}