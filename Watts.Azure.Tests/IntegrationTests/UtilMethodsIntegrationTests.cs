namespace Watts.Azure.Tests.IntegrationTests
{
    using Common.Interfaces.General;
    using Common.Storage.Objects;
    using NUnit.Framework;
    using Objects;

    /// <summary>
    /// Tests various utility methods, e.g. logging methods.
    /// </summary>
    [TestFixture]
    public class UtilMethodsIntegrationTests
    {
        private ILog logUnderTest;

        private TestEnvironmentConfig testEnvironmentCredentials;

        /// <summary>
        ///  Create the log under test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.testEnvironmentCredentials = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.logUnderTest = new LogTableStorage(this.testEnvironmentCredentials.FileshareConnectionString, "UtilMethodsIntegrationTests");
        }

        /// <summary>
        /// Tests that it is possible to use the log to write a debug statement.
        /// </summary>
        [Category("IntegrationTest")]
        [Test]
        public void Log_WriteDebug_WritesDebugToLog()
        {
            this.logUnderTest.Debug("some debug statement (Integration test)");
        }
    }
}