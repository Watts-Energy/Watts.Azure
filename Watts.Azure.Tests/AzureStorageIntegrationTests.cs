namespace Watts.Azure.Tests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Tests.Objects;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// Tests related to storing and retrieving data in Azure.
    /// </summary>
    [TestClass]
    public class AzureStorageIntegrationTests
    {
        private IBatchEnvironment environment;
        private AzureBlobStorage blobStorageUnderTest;

        private TestEnvironmentConfig config;

        [TestInitialize]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.environment = this.config.BatchEnvironment;

            this.blobStorageUnderTest = AzureBlobStorage.Connect(this.environment.GetBatchStorageConnectionString(), "testcontainer");
        }

        /// <summary>
        /// Tests that it is possible to store a blob (a text file) to the storage account being tested.
        /// </summary>
        [TestMethod]
        [TestCategory("IntegrationTest"), TestCategory("Azure Blob Storage")]
        public void StoreBlob()
        {
            // ARRANGE
            string blobName = "myBlob.txt";
            string tempFile = "TempFile.txt";

            string[] fileLines = new[] { "line1", "line2" };

            File.WriteAllLines(tempFile, fileLines);

            // ACT
            this.blobStorageUnderTest.UploadFromFile(tempFile, blobName);

            var downloadedBlob = this.blobStorageUnderTest.GetBlobContents(blobName);
            var splitContent = downloadedBlob.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // ASSERT
            Assert.AreEqual(2, splitContent.Length);
            Assert.AreEqual(fileLines[0], splitContent[0]);
            Assert.AreEqual(fileLines[1], splitContent[1]);

            // CLEAN UP
            this.blobStorageUnderTest.DeleteContainerIfExists();
        }

        /// <summary>
        /// Tests that it is possible to upload a file to a file share in Azure.
        /// </summary>
        [TestCategory("IntegrationTest"), TestCategory("Azure File Storage")]
        [TestMethod]
        public void TestUploadFile()
        {
            // ARRANGE
            string filename = "TestUploadFile.txt";
            File.WriteAllText(filename, "This is an automated test");
            string sharename = "integration-test-share";

            // ACT
            AzureFileshare share = AzureFileshare.Connect(this.config.FileshareConnnectionString, sharename);
            share.CreateIfDoesntExist(this.config.TestFileShareAccount.Credentials.AccountName, this.config.FileshareAccountKey);
            share.SaveDataToFile(filename);

            // ASSERT
            string localFileCopyName = "TestUploadFile_Downloaded.txt";
            share.DownloadFile(filename, localFileCopyName);

            Assert.AreEqual(File.ReadAllText(filename), File.ReadAllText(localFileCopyName));

            // Delete the local file
            File.Delete(filename);
        }
    }
}