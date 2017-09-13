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

    [TestClass]
    public class DataFactoryIntegrationTests
    {
        private PredefinedDataCopyEnvironment environment;
        private AzureActiveDirectoryAuthentication dataFactoryAuthentication;

        [TestInitialize]
        public void Setup()
        {
            this.environment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataCopyEnvironment;
            
            this.dataFactoryAuthentication = new AzureActiveDirectoryAuthentication(
                this.environment.SubscriptionId,
                new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = this.environment.AdfClientId,
                    ClientSecret = this.environment.ClientSecret,
                    TenantId = this.environment.ActiveDirectoryTenantId
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

            sourceTable.Insert<TestEntity>(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()));

            // Perform the copy.
            DataCopyBuilder
                .InDataFactoryEnvironment(this.environment)
                .UsingDataFactorySetup(this.environment.DataFactorySetup)
                .UsingDefaultCopySetup()
                .WithTimeoutInMinutes(20)
                .AuthenticateUsing(this.dataFactoryAuthentication)
                .CopyFromTable(sourceTable)
                .WithSourceQuery(null)
                .ToTable(targetTable)
                .ReportProgressToConsole()
                .StartCopy();

            // Assert that numberOfEntities are now present in the target table.
            var entitiesInTargetTable = targetTable.GetTop(1000);

            Assert.AreEqual(10, entitiesInTargetTable.Count);
        }

        [TestCategory("IntegrationTest")]
        [TestCategory("DataLake")]
        [TestMethod]
        public void CopySimpleData_FromTable_ToDataLake()
        {

        }
    }
}