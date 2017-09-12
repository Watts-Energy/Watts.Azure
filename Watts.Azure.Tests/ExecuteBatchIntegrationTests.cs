namespace Watts.Azure.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common;
    using Watts.Azure.Common.Storage.Objects.Wrappers;
    using Watts.Azure.Tests.Objects;
    using Watts.Azure.Utils.Build;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// Various integration tests of batch.
    /// </summary>
    [TestClass]
    public class ExecuteBatchIntegrationTests
    {
        /// <summary>
        /// The batch environment
        /// </summary>
        private IPredefinedBatchEnvironment environment;

        /// <summary>
        /// Setup the test by creating the environment.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Set your environment here (i.e. create a class implementing IPredefinedBatchEnvironment and instantiate it here.
            // The default is to load it from a config file (located in the root of the test project and named 'testEnvironment.testenv').
            // The format is json and the extension .testenv is added to gitignore. Make sure you never commit that file, as that would share your
            // credentials.
            this.environment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().BatchEnvironment;
        }

        /// <summary>
        /// Tests that it is possible to find a ubuntu image in the batch marketplace.
        /// </summary>
        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void GetUbuntuBox_FindsImage()
        {
            // ARRANGE
            var credentials = BatchBuilder.InPredefinedEnvironment(this.environment).Credentials;

            AzureBatchClient client = new AzureBatchClient(credentials);

            // ACT
            var image = PredefinedMachines.GetUbuntu14_04VmConfiguration(client);

            // ASSERT
            Assert.IsNotNull(image);
        }

        /// <summary>
        /// Tests that it is possible to find a debian image in the batch marketplace.
        /// </summary>
        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void GetDebian8Box_FindsImage()
        {
            // ARRANGE
            var credentials = BatchBuilder.InPredefinedEnvironment(this.environment).Credentials;

            AzureBatchClient client = new AzureBatchClient(credentials);

            // ACT
            var image = PredefinedMachines.GetDebian8VmConfiguration(client);

            // ASSERT
            Assert.IsNotNull(image);
        }
    }
}