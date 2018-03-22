namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Common.DataFactory.Copy;
    using Common.Interfaces.Storage;
    using Common.Security;
    using Common.Storage.Objects;
    using FluentAssertions;
    using NUnit.Framework;
    using Objects;
    using Utils.Build;
    using Utils.Objects;

    [TestFixture]
    public class DataStructureIntegrationTests
    {
        private TestEnvironmentConfig config;

        private DataCopyEnvironment environment;

        private AzureActiveDirectoryAuthentication dataFactoryAuthentication;

        [SetUp]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.environment = this.config.DataCopyEnvironment;

            this.dataFactoryAuthentication = new AzureActiveDirectoryAuthentication(
                this.environment.SubscriptionId,
                string.Empty,
                this.environment.Credentials);

        }

        /// <summary>
        /// Tests that it is possible to modify the structure of entities in a table by specifying it directly through the fluent interface, to e.g.
        /// remove an existing property due to a change in the code data model.
        /// </summary>
        [Category("IntegrationTest")]
        [Test]
        public void ModifyStructureOfExistingTable()
        {
            IAzureTableStorage sourceStorage = new AzureTableStorage("TestTable123", this.environment.GetDataFactoryStorageAccountConnectionString());
            sourceStorage.DeleteIfExists();

            IAzureTableStorage targetStorage = new AzureTableStorage("TestTable123Backup", this.environment.GetDataFactoryStorageAccountConnectionString());
            targetStorage.DeleteIfExists();

            var testEntities = TestFacade.RandomEntities(100, Guid.NewGuid().ToString());

            sourceStorage.Insert(testEntities, null);

            CopySetup setup = new CopySetup()
            {
                CopyPipelineName = "ModifyStructureTest",
                CreateTargetIfNotExists = true,
                SourceDatasetName = "TestDataSource",
                SourceLinkedServiceName = "TestTableStorage",
                TargetDatasetName = "TestDataCopy",
                TargetLinkedServiceName = "TestDataCopy",
                TimeoutInMinutes = 20
            };

            DataCopyBuilder.InDataFactoryEnvironment(this.environment)
                .UsingDataFactorySetup(this.environment.DataFactorySetup)
                .UsingCopySetup(setup)
                .AuthenticateUsing(this.dataFactoryAuthentication)
                .CopyFrom(sourceStorage)
                .To(targetStorage)
                .ReportProgressTo(progress => Trace.WriteLine(progress))
                .StartCopy();

            // Verify that the source data has all properties present on 'TestEntity'.
            var firstEntityBefore = sourceStorage.GetTop(1);

            int numberOfPropertiesOnTestEntityType = 5;
            firstEntityBefore.First().Properties.Count.Should().Be(numberOfPropertiesOnTestEntityType, "because the entities are of type TestEntity which has five properties");

            // The data has now been copied from the source to the target.
            // Move it back from the target to the source. This should cause the data to lose the column 'Value' as that is not part of the
            // type TestEntityMissingOneProperty, which we'll specify should be the format.
            DataCopyBuilder.InDataFactoryEnvironment(this.environment)
                .UsingDataFactorySetup(this.environment.DataFactorySetup)
                .UsingCopySetup(setup)
                .AuthenticateUsing(this.dataFactoryAuthentication)
                .CopyFrom(targetStorage)
                .To(sourceStorage)
                .ReportProgressTo(progress => Trace.WriteLine("moving back: " + progress))
                .StructuredAs<TestEntityMissingOneProperty>()
                .StartCopy();

            var someEntityFromSource = sourceStorage.GetTop(1);

            // ASSERT that the property "Value" is not on the entities in the source, since they have been removed
            // when we specified StructureAs<TestEntityMissingOneProperty>
            int expectedNumberOfProperties = 4; // Id, Key, Name, Date
            var firstEntity = someEntityFromSource.First();
            firstEntity.Properties.Count.Should().Be(expectedNumberOfProperties, "because we've removed one property from the table by specifying a specific structure");
            firstEntity.Properties.Select(p => p.Key).Should().NotContain(nameof(TestEntity.Value), "because the 'Value' property is the one that should have been removed");

        }
    }
}