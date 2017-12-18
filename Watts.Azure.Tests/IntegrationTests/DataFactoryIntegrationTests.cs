namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Azure.Utils.Build;
    using Azure.Utils.Objects;
    using Common.DataFactory.Copy;
    using Common.Security;
    using Common.Storage.Objects;
    using FluentAssertions;
    using NUnit.Framework;
    using Objects;

    [TestFixture]
    public class DataFactoryIntegrationTests
    {
        private DataCopyEnvironment environment;
        private DataLakeStoreEnvironment dataLakeEnvironment;
        private AzureActiveDirectoryAuthentication dataFactoryAuthentication;
        private AzureActiveDirectoryAuthentication dataLakeAuthentication;

        [SetUp]
        public void Setup()
        {
            this.environment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataCopyEnvironment;

            this.dataLakeEnvironment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataLakeEnvironment;

            this.dataFactoryAuthentication = new AzureActiveDirectoryAuthentication(
                this.environment.SubscriptionId,
                string.Empty,
                this.environment.Credentials);

            this.dataLakeAuthentication = new AzureActiveDirectoryAuthentication(
                this.dataLakeEnvironment.SubscriptionId,
                this.dataLakeEnvironment.ResourceGroupName,
                this.dataLakeEnvironment.Credentials);
        }

        [Category("IntegrationTest"), Category("DataFactory")]
        [Test]
        public void CopyData_SimpleDataCopyTableToTable()
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

            sourceTable.Insert(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()), null);

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
            entitiesInTargetTable.Count.Should().Be(numberOfEntities,
                "because we expect to find all entities copied to the target table");
        }

        /// <summary>
        /// Tests that copying data from a table to a data lake works. A table is populated with a number of entities, which are then copied to data lake.
        /// The resulting file in the data lake store is then downloaded and the number of lines compared with the number of entities inserted into the table.
        /// </summary>
        [Category("IntegrationTest")]
        [Category("DataFactory")]
        [Test]
        public void CopySimpleData_FromTable_ToDataLake()
        {
            // ARRANGE
            AzureTableStorage sourceTable = AzureTableStorage.Connect(this.environment.GetDataFactoryStorageAccountConnectionString(), "SourceTableDataLakeTest");
            AzureDataLakeStore targetDataLake = new AzureDataLakeStore("/copydatatest", this.dataLakeEnvironment.DataLakeStoreName, this.dataLakeAuthentication);

            var deleted = sourceTable.DeleteIfExists();
            if (deleted)
            {
                // Sleep a minute to ensure that Azure has actually deleted the table
                Thread.Sleep(60000);
            }

            int numberOfEntities = 10;

            sourceTable.Insert(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()), null);

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

            // ASSERT
            contents.Length.Should().Be(numberOfEntities + 1, $"because we expect to find one line for each entity we inserted in the source ({numberOfEntities}) and one for the header");

            // Clean up
            targetDataLake.DeleteDirectory(string.Empty, true).Wait();
            sourceTable.DeleteIfExists();
            File.Delete(downloadFileName);
        }
    }
}