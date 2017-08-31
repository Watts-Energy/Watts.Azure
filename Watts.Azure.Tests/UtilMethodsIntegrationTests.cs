namespace Watts.Azure.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common.Interfaces.General;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Tests.Objects;

    /// <summary>
    /// Tests various utility methods, e.g. logging methods.
    /// </summary>
    [TestClass]
    public class UtilMethodsIntegrationTests
    {
        private ILog logUnderTest;

        private TestEnvironmentConfig testEnvironmentCredentials;

        /// <summary>
        ///  Create the log under test.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            this.testEnvironmentCredentials = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.logUnderTest = new LogTableStorage(this.testEnvironmentCredentials.StorageAccount, "UtilMethodsIntegrationTests");
        }

        /// <summary>
        /// Tests that it is possible to use the log to write a debug statement.
        /// </summary>
        [TestCategory("IntegrationTest")]
        [TestMethod]
        public void Log_WriteDebug_WritesDebugToLog()
        {
            this.logUnderTest.Debug("some debug statement (Integration test)");
        }
    }
}