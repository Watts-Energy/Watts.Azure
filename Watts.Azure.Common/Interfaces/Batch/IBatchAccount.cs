namespace Watts.Azure.Common.Interfaces.Batch
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;
    using Wrappers;

    /// <summary>
    /// Interface for a Batch account
    /// </summary>
    public interface IBatchAccount : IDisposable
    {
        /// <summary>
        /// A (optional) delegate on which the batch account may report progress.
        /// </summary>
        Action<string> ProgressDelegate { get; set; }

        IAzureBatchClient BatchClient { get; }

        Task<List<ResourceFile>> UploadFilesToContainerAsync(IAzureBlobClient blobClient, string inputContainerName, List<string> filePaths);

        Task<ResourceFile> UploadFileToContainerAsync(IAzureBlobClient blobClient, string containerName, string filePath);

        Task CreateContainerIfNotExistAsync(IAzureBlobClient blobClient, string containerName);

        Task<CloudPool> CreatePoolAsync(string poolId, IList<ResourceFile> resourceFiles, IList<ApplicationPackageReference> applicationReferences = null);

        Task<CloudJob> CreateJobAsync(string jobId, string poolId);

        Task<List<CloudTask>> AddTasksAsync(string jobId, List<ResourceFile> inputFiles, IList<ApplicationPackageReference> packageReferences = null);

        Task<bool> MonitorTasks(string jobId, TimeSpan timeout);

        Task<List<ComputeNode>> GetComputeNodes();

        Task DownloadBlobsFromContainerAsync(IAzureBlobClient blobClient, string containerName, string directoryPath);

        Task DeleteContainerAsync(IAzureBlobClient blobClient, string containerName);
    }
}