namespace Watts.Azure.Tests.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Common.Batch.Jobs;
    using Common.Batch.Objects;
    using Common.Interfaces.Batch;
    using Common.Interfaces.General;
    using Common.Interfaces.Wrappers;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch;
    using Moq;
    using NUnit.Framework;
    using Objects;

    /// <summary>
    /// Various unit tests of the BatchExecutionBase class.
    /// </summary>
    [TestFixture]
    public class BatchExecutionBaseUnitTests
    {
        private Mock<IBatchAccount> mockBatchAccount;
        private Mock<IBatchExecutionSettings> mockExecutionSettings;
        private Mock<IPrepareInputFiles> mockPrepareInput;
        private Mock<IBatchDependencyResolver> mockDependencyResolver;
        private Mock<ICloudAccountFactory> mockCloudAccountFactory;
        private Mock<IAzureBlobClient> mockBlobClient;
        private Mock<IAzureBatchClient> mockAzureBatchClient;
        private Mock<IJobOperations> mockJobOperations;
        private Mock<IPoolOperations> mockPoolOperations;

        private BatchExecutionBase executionUnderTest;

        /// <summary>
        /// Create mocks of the dependencies of BatchExecutionBase and set up default stubs.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.mockBatchAccount = new Mock<IBatchAccount>();
            this.mockExecutionSettings = new Mock<IBatchExecutionSettings>();
            this.mockPrepareInput = new Mock<IPrepareInputFiles>();
            this.mockDependencyResolver = new Mock<IBatchDependencyResolver>();
            this.mockCloudAccountFactory = new Mock<ICloudAccountFactory>();
            this.mockBlobClient = new Mock<IAzureBlobClient>();
            this.mockAzureBatchClient = new Mock<IAzureBatchClient>();
            this.mockAzureBatchClient = new Mock<IAzureBatchClient>();
            this.mockPoolOperations = new Mock<IPoolOperations>();

            this.mockBatchAccount.Setup(p => p.BatchClient).Returns(this.mockAzureBatchClient.Object);

            this.mockJobOperations = new Mock<IJobOperations>();
            this.mockPoolOperations = new Mock<IPoolOperations>();

            this.mockAzureBatchClient.Setup(p => p.JobOperations).Returns(this.mockJobOperations.Object);
            this.mockAzureBatchClient.Setup(p => p.PoolOperations).Returns(this.mockPoolOperations.Object);

            var mockStorageAccount = new Mock<IStorageAccount>();
            this.mockExecutionSettings.Setup(p => p.BatchStorageAccountSettings).Returns(new StorageAccountSettings()
            {
                StorageAccountName = string.Empty,
                StorageAccountKey = string.Empty
            });

            this.mockExecutionSettings.Setup(p => p.ExecutableInfos).Returns(new List<BatchExecutableInfo>());

            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(false);

            this.mockExecutionSettings.Setup(p => p.BatchPoolSetup).Returns(new BatchPoolSetup()
            {
                PoolId = string.Empty,
                JobId = string.Empty
            });

            this.mockExecutionSettings.Setup(p => p.ExecutableInfos)
               .Returns(new List<BatchExecutableInfo>()
               {
                    new BatchExecutableInfo()
                    {
                        BatchExecutableContainerName = string.Empty,
                        BatchInputContainerName = string.Empty
                    }
               });

            mockStorageAccount.Setup(p => p.CreateCloudBlobClient()).Returns(this.mockBlobClient.Object);

            this.mockCloudAccountFactory.Setup(p => p.GetStorageAccount(It.IsAny<string>()))
                .Returns(mockStorageAccount.Object);

            // Mock the ListJobs method to return some empty list of jobs.
            MockPagedEnumerable<CloudJob> mockPagedEnumerator = new MockPagedEnumerable<CloudJob>();
            this.mockJobOperations
                .Setup(p => p.ListJobs(It.IsAny<DetailLevel>(), It.IsAny<IEnumerable<BatchClientBehavior>>()))
                .Returns(mockPagedEnumerator);

            this.executionUnderTest = new BatchExecutionBase(this.mockBatchAccount.Object, this.mockExecutionSettings.Object, this.mockPrepareInput.Object, this.mockDependencyResolver.Object, this.mockCloudAccountFactory.Object, null);
        }

        /// <summary>
        /// Tests that when the execution settings are invalid, an exception is thrown.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_SettingsAreInvalid_ThrowsExeption()
        {
            // ARRANGE: Stub isvalid to return false - this should cause an exception to be thrown.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(false);

            // ACT AND ASSERT that an exception is thrown.
            Assert.Throws<AggregateException>(() => this.executionUnderTest.StartBatch().Wait());
        }

        /// <summary>
        /// Tests that when the settings are valid, the dependency resolver's Resolve method is invoked exactly once.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_SettingsAreValidAndContainersCreated_InvokesDependencyResolverExactlyOnce()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockDependencyResolver.Verify(p => p.Resolve(), Times.Once());
        }

        /// <summary>
        /// Tests that when the settings are valid, the dependency resolver's Resolve method is invoked exactly once.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_SettingsAreValidAndContainersCreated_InvokesPrepareInputFilesOnce()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockPrepareInput.Verify(p => p.PrepareFiles(), Times.Once());
        }

        /// <summary>
        /// Tests that when the settings are valid, the UploadFilesToContainerAsync method on the account is invoked exactly twice, once for application files
        /// and once for the input files.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_SettingsAreValidAndContainersCreated_UploadFilesToContainerAsyncTwice()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockBatchAccount.Verify(p => p.UploadFilesToContainerAsync(It.IsAny<IAzureBlobClient>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Exactly(2));
        }

        /// <summary>
        /// Tests that when the settings are valid, the CreateContainerIfNotExistAsync method on the account is invoked exactly twice, once for application files
        /// and once for the input files.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_SettingsAreValidAndContainersCreated_CreateContainerIfNotExistAsyncTwice()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockBatchAccount.Verify(p => p.CreateContainerIfNotExistAsync(It.IsAny<IAzureBlobClient>(), It.IsAny<string>()), Times.Exactly(2));
        }

        /// <summary>
        /// Tests that when the CleanUpAfterwards property on the execution settings is true, the DeleteContainerAsyncIsInvoked method on the account is invoked exactly twice, once for application files
        /// and once for the input file containers.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsTrue_DeleteContainerAsyncIsInvokedTwice()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockBatchAccount.Verify(p => p.DeleteContainerAsync(It.IsAny<IAzureBlobClient>(), It.IsAny<string>()), Times.Exactly(2));
        }

        /// <summary>
        /// Tests that when the CleanUpAfterwards property on the execution settings if FALSE, the DeleteContainerAsync method on the batch account is NOT invoked.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsFalse_DeleteContainerAsyncIsNotInvoked()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.ExecutableInfos)
                .Returns(new List<BatchExecutableInfo>()
                {
                    new BatchExecutableInfo()
                    {
                        BatchExecutableContainerName = string.Empty,
                        BatchInputContainerName = string.Empty
                    }
                });

            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(false);

            

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockBatchAccount.Verify(p => p.DeleteContainerAsync(It.IsAny<IAzureBlobClient>(), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Tests that when CleanUpAfterwards is true, the DeletePoolAsync method on the pool operations is invoked exactly once.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsTrue_DeletePoolAsyncInvokedExactlyOnce()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockPoolOperations.Verify(p => p.DeletePoolAsync(It.IsAny<string>(), It.IsAny<IEnumerable<BatchClientBehavior>>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        /// <summary>
        /// Tests that when when CleanUpAfterwards is false, the DeletePoolAsync method on the pooloperations is never invoked.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsFalse_DeletePoolAsyncIsNeverInvoked()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(false);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockPoolOperations.Verify(p => p.DeletePoolAsync(It.IsAny<string>(), It.IsAny<IEnumerable<BatchClientBehavior>>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        /// <summary>
        /// Tests that when CleanUpAfterwards is true, the DeleteJobAsync method on the job operations is invoked exactly once.
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsTrue_DeleteJobAsyncInvokedExactlyOnce()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(true);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockJobOperations.Verify(p => p.DeleteJobAsync(It.IsAny<string>()), Times.Once());
        }

        /// <summary>
        /// Tests that when CleanUpAfterwards is false, the DeleteJobAsync method on the account is never invoked
        /// </summary>
        [Test]
        [Category("UnitTest")]
        public void ExecuteAsync_CleanUpSettingsAfterwardsFalse_DeleteJobAsyncIsNeverInvoked()
        {
            // ARRANGE: Ensure that the execution settings return valid, which makes sure execution doesn't halt directly.
            this.mockExecutionSettings.Setup(p => p.IsValid).Returns(true);
            this.mockExecutionSettings.Setup(p => p.CleanupAfterwards).Returns(false);

            // ACT
            this.executionUnderTest.StartBatch().Wait();

            // ASSERT
            this.mockJobOperations.Verify(p => p.DeleteJobAsync(It.IsAny<string>()), Times.Never());
        }
    }
}