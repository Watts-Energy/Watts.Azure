namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Azure.Utils.Objects;
    using Common.Security;
    using Common.Storage.Objects;
    using FluentAssertions;
    using Microsoft.Azure.Management.DataLake.Store.Models;
    using NUnit.Framework;
    using Objects;

    [TestFixture]
    public class DataLakeIntegrationTests
    {
        private DataLakeStoreEnvironment dataLakeEnvironment;
        private AzureActiveDirectoryAuthentication dataLakeAuthentication;
        private AzureDataLakeStore dataLake;

        /// <summary>
        /// Set up by creating the data lake store.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.dataLakeEnvironment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().DataLakeEnvironment;

            this.dataLakeAuthentication = new AzureActiveDirectoryAuthentication(
                this.dataLakeEnvironment.SubscriptionId,
                this.dataLakeEnvironment.ResourceGroupName,
                this.dataLakeEnvironment.Credentials);

            // Create a data lake store with root /
            this.dataLake = new AzureDataLakeStore(string.Empty, this.dataLakeEnvironment.DataLakeStoreName, this.dataLakeAuthentication);
        }

        /// <summary>
        /// Tests that it is possible to create a file in the data lake
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_CreateFile()
        {
            // ARRANGE
            string fileName = "integrationtest_create_file.txt";
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryName = "/integrationtest_datalake_createfile";
            string outFile = Path.Combine(testDirectory, fileName);
            File.WriteAllText(outFile, "hello world!");

            // Create the directory
            this.dataLake.CreateDirectory(directoryName).Wait();

            // ACT
            this.dataLake.UploadFile(outFile, string.Join("/", directoryName, fileName), true);

            List<FileStatusProperties> itemsInDirectory = this.dataLake.ListItems(directoryName);
            Func<bool> action = () => itemsInDirectory.Any(p => p.PathSuffix.Equals(fileName));

            // ASSERT
            action().Should().Be(true, "because the function returns true if the directory contains our uploaded file and the directory should contain it");

            // Clean up
            this.dataLake.DeleteDirectory(directoryName, true).Wait();
            File.Delete(outFile);
        }

        /// <summary>
        /// Tests that it is possible to create a directory
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_CreateDirectory()
        {
            // ARRANGE
            string directoryName = "/integrationtest_datalake_createdirectory";
            AzureDataLakeStore dataLake = new AzureDataLakeStore(string.Empty, this.dataLakeEnvironment.DataLakeStoreName, this.dataLakeAuthentication);

            // Delete the directory if it exists...
            dataLake.DeleteDirectory(directoryName, true).Wait();

            // Define a function that returns true if the root directory contains the directory and false otherwise.
            Func<bool> checkExistanceDelegate = () => dataLake.ListItems("/").Any(p => p.PathSuffix.Equals(directoryName.Substring(1)));

            // Check that the directory doesn't exist
            checkExistanceDelegate().Should().Be(false, "because we've just ensured this directory does not exist by deleting it and listing the root should not list this directory");

            // ACT
            dataLake.CreateDirectory(directoryName).Wait();

            // ASSERT
            checkExistanceDelegate().Should().Be(true, "because we just created the directory and listing the root should list this directory");

            // Clean up
            dataLake.DeleteDirectory(directoryName, true).Wait();
        }

        /// <summary>
        /// Tests that when deleting a directory that is not empty, and specifying that the delete is NOT recursive, an exception is thrown.
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteDirectoryWhenNotEmptyWithoutForce_ThrowsException()
        {
            // ARRANGE
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryName = "integrationtest_deletedirectorywithoutforce";
            this.dataLake.CreateDirectory(directoryName).Wait();
            string fileName = "somefile.txt";
            string filePath = Path.Combine(testDirectory, fileName);
            File.WriteAllText(filePath, "some content");
            this.dataLake.UploadFile(filePath, string.Join("/", directoryName, fileName));

            // Assert that attempting to delete the directory with recursive:false while not empty throws an exception.
            Action deleteDirectoryWithoutForce = () => this.dataLake.DeleteDirectory(directoryName, recursive: false).Wait();

            deleteDirectoryWithoutForce.ShouldThrow<Exception>(
                "because we should get an exception when attempting to delete a non-empty directory without specifying force:true");

            // Assert that the directory was not deleted
            this.dataLake.PathExists(directoryName).Should().Be(true, "because we should not have been allowed to delete the directory so PathExists should return true");

            // Clean up
            this.dataLake.DeleteDirectory(directoryName, true).Wait();
            File.Delete(filePath);
        }

        /// <summary>
        /// Tests that when deleting a non-empty directory with the recursive argument set to true, the delete works.
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteDirectoryWhenNotEmpty_WithForce()
        {
            // ARRANGE
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryName = "integrationtest_deletedirectorywithforce";
            this.dataLake.CreateDirectory(directoryName).Wait();
            string fileName = "somefile.txt";
            string filePath = Path.Combine(testDirectory, fileName);
            File.WriteAllText(filePath, "some content");
            this.dataLake.UploadFile(filePath, string.Join("/", directoryName, fileName));

            this.dataLake.DeleteDirectory(directoryName, recursive: true).Wait();

            // Assert that the directory was not deleted
            this.dataLake.PathExists(directoryName).Should().Be(false, "because we expect the directory to have been deleted, even though it was not empty, since we specified 'force':true");

            // Clean up
            File.Delete(filePath);
        }

        /// <summary>
        /// Tests that a data lake store can delete the directory which is set to be its root directory.
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteDirectoryWhichIsRoot()
        {
            AzureDataLakeStore lakeWithNonRootDirectory = new AzureDataLakeStore("/deletemedir", this.dataLakeEnvironment.DataLakeStoreName, this.dataLakeAuthentication);

            lakeWithNonRootDirectory.CreateDirectory(string.Empty).Wait();

            Assert.IsTrue(lakeWithNonRootDirectory.PathExists(lakeWithNonRootDirectory.Directory));

            lakeWithNonRootDirectory.DeleteDirectory(string.Empty, true).Wait();

            lakeWithNonRootDirectory.PathExists(lakeWithNonRootDirectory.Directory).Should().Be(false, "because we have just deleted the directory and PathExists should now return false");
        }

        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteFile()
        {
            // ARRANGE
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryName = "integrationtest_deletefile";
            string localFileName = "filetodelete.txt";
            string localFilePath = Path.Combine(testDirectory, localFileName);
            File.WriteAllText(localFilePath, "content");
            string dataLakeFilePath = directoryName + "/" + localFileName;

            // ACT
            this.dataLake.CreateDirectory(directoryName).Wait();
            this.dataLake.UploadFile(localFilePath, dataLakeFilePath);

            this.dataLake.DeleteFile(dataLakeFilePath).Wait();

            // ASSERT
            this.dataLake.PathExists(dataLakeFilePath).Should().Be(false, "because we have invoked delete on the file, and it should now no longer exist");

            // Clean up
            this.dataLake.DeleteDirectory(directoryName, true).Wait();
            File.Delete(localFilePath);
        }

        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteFileOnDirectory_Fails()
        {
            string directoryname = "integrationtest_deletefileondirectory";
            this.dataLake.CreateDirectory(directoryname).Wait();

            Action action = () => this.dataLake.DeleteFile(directoryname).Wait();

            action.ShouldThrow<ArgumentException>(
                "because we should get an argumentexception when invoking DeleteFile on a directory");

            // Clean up
            this.dataLake.DeleteDirectory(directoryname).Wait();
        }

        /// <summary>
        /// Tests that invoking DeleteDirectory on a file throws an exception.
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_DeleteDirectoryOnFile_Fails()
        {
            // ARRANGE
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryname = "integrationtest_deletedirectoryonfile";
            this.dataLake.CreateDirectory(directoryname).Wait();
            string localFileName = Guid.NewGuid() + ".txt";
            string localFilePath = Path.Combine(testDirectory, localFileName);
            File.WriteAllText(localFilePath, "testing");
            string dataLakeFilePath = directoryname + "/" + localFileName;

            // ACT
            this.dataLake.CreateDirectory(directoryname).Wait();
            this.dataLake.UploadFile(localFilePath, dataLakeFilePath, true);

            // ASSERT that invoking delete directory on a file throws an ArgumentException
            Action actionShouldThrow = () => this.dataLake.DeleteDirectory(dataLakeFilePath).Wait();
            actionShouldThrow.ShouldThrow<Exception>(
                "because invoking DeleteDirectory on a file should throw an exception");

            // Clean up
            this.dataLake.DeleteDirectory(directoryname, true).Wait();
        }

        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_ConcatenateFiles()
        {
            // ARRANGE. Write two local files, upload them to the data lake store and prepare paths and filenames
            string firstFileName = "concatFile1.txt";
            string secondFileName = "concatFile2.txt";
            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string localFilePath1 = Path.Combine(testDirectory, "concatFile1.txt");
            string localFilePath2 = Path.Combine(testDirectory, "concatFile2.txt");
            string dataLakeDirectoryName = "/integrationtest_concatenatefiles";
            string dataLakeConcatFileName = string.Join("/", dataLakeDirectoryName, "concatFile.txt");
            string dataLakeFileName1 = string.Join("/", dataLakeDirectoryName, firstFileName);
            string dataLakeFileName2 = string.Join("/", dataLakeDirectoryName, secondFileName);
            string downloadFileName = Path.Combine(testDirectory, "concatFile.txt");

            File.WriteAllText(localFilePath1, "Hello");
            File.WriteAllText(localFilePath2, "World");

            this.dataLake.CreateDirectory(dataLakeDirectoryName).Wait();
            this.dataLake.UploadFile(localFilePath1, dataLakeFileName1);
            this.dataLake.UploadFile(localFilePath2, dataLakeFileName2);

            // Delete the file to combine the other files in, if it exists
            this.dataLake.DeleteFile(dataLakeConcatFileName).Wait();

            // ACT
            this.dataLake.ConcatenateFiles(new string[] { dataLakeFileName1, dataLakeFileName2 }, dataLakeConcatFileName).Wait();

            // Download the file and check the file contents match the concatenation of the two local files.
            this.dataLake.DownloadFile(dataLakeConcatFileName, downloadFileName);

            string fileContents = File.ReadAllText(downloadFileName);

            // ASSERT that the file contents match the concatenation of the uploaded files
            fileContents.Should().StartWith("Hello").And.EndWith("World");

            // Clean up remote and local files/directories
            this.dataLake.DeleteDirectory(dataLakeDirectoryName, true).Wait();
            File.Delete(localFilePath1);
            File.Delete(localFilePath2);
            File.Delete(downloadFileName);
        }

        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_AppendToFile()
        {
            string fileName = "appendToThisFile.txt";

            string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // ARRANGE. Create file names and write the local file
            string localFilePath = Path.Combine(testDirectory, fileName);
            string dataLakeDirectoryName = "/integrationtest_appendtofile";
            string contentInOriginalFile = "Hello";
            string contentToAppend = "World";
            string dataLakeFileName = string.Join("/", dataLakeDirectoryName, fileName);
            string downloadFileName = Path.Combine(testDirectory, "appendedToFile.txt");

            File.WriteAllText(localFilePath, contentInOriginalFile);

            // Create the directory, upload the file, append to it and download it.
            this.dataLake.CreateDirectory(dataLakeDirectoryName).Wait();
            this.dataLake.UploadFile(localFilePath, dataLakeFileName, true);
            this.dataLake.AppendToFile(dataLakeFileName, contentToAppend).Wait();
            this.dataLake.DownloadFile(dataLakeFileName, downloadFileName);

            string contents = File.ReadAllText(downloadFileName);

            // ASSERT that the contents of the file contain the original + the appended text
            contents.Should()
                .StartWith(contentInOriginalFile, "because the file originally contained this")
                .And
                .EndWith(contentToAppend, "because we have appended this content");

            // Clean up
            this.dataLake.DeleteDirectory(dataLakeDirectoryName, true).Wait();
            File.Delete(localFilePath);
            File.Delete(downloadFileName);
        }

        /// <summary>
        /// Tests that listing the contents of the root directory does not throw an exception.
        /// </summary>
        [Category("IntegrationTest"), Category("DataLake")]
        [Test]
        public void DataLake_ListDirectories()
        {
            Action action = () => this.dataLake.ListItems("/");

            action.ShouldNotThrow<Exception>();
        }
    }
}