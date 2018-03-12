namespace Watts.Azure.Common.Batch.Objects
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using General;
    using Interfaces.Batch;
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Represents a batch account in Azure. Gives access to creating/deleting containers and pools, creating jobs and tasks and thereby running batch jobs.
    /// </summary>
    public class BatchAccount : IBatchAccount
    {
        private readonly IBatchExecutionSettings executionSettings;

        public BatchAccount(IBatchExecutionSettings executionSettings, IAzureBatchClient batchClient, Action<string> callbackProgressDelegate = null)
        {
            this.executionSettings = executionSettings;
            this.BatchClient = batchClient;
            this.ProgressDelegate = callbackProgressDelegate;
        }

        public CloudPool Pool { get; set; }

        public IAzureBatchClient BatchClient { get; set; }

        public Action<string> ProgressDelegate { get; set; }

        /// <summary>
        /// Uploads the specified files to the specified Blob container, returning a corresponding
        /// collection of <see cref="ResourceFile"/> objects appropriate for assigning to a task's
        /// <see cref="CloudTask.ResourceFiles"/> property.
        /// </summary>
        /// <param name="blobClient">A <see cref="Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient"/>.</param>
        /// <param name="inputContainerName"></param>
        /// <param name="filePaths">A collection of paths of the files to be uploaded to the container.</param>
        /// <returns>A collection of <see cref="ResourceFile"/> objects.</returns>
        public async Task<List<ResourceFile>> UploadFilesToContainerAsync(IAzureBlobClient blobClient, string inputContainerName, List<string> filePaths)
        {
            List<ResourceFile> resourceFiles = new List<ResourceFile>();

            foreach (string filePath in filePaths)
            {
                resourceFiles.Add(await this.UploadFileToContainerAsync(blobClient, inputContainerName, filePath));
            }

            return resourceFiles;
        }

        /// <summary>
        /// Uploads the specified file to the specified Blob container.
        /// </summary>
        /// <param name="blobClient">A <see cref="Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient"/>.</param>
        /// <param name="containerName">The name of the blob storage container to which the file should be uploaded.</param>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <returns>A <see cref="Microsoft.Azure.Batch.ResourceFile"/> instance representing the file within blob storage.</returns>
        public async Task<ResourceFile> UploadFileToContainerAsync(IAzureBlobClient blobClient, string containerName, string filePath)
        {
            this.Report("Uploading file {0} to container [{1}]...", filePath, containerName);

            string blobName = Path.GetFileName(filePath);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromFileAsync(filePath);

            // Set the expiry time and permissions for the blob shared access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(this.executionSettings.TimeoutInMinutes),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // Construct the SAS URL for blob
            string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
            string blobSasUri = $"{blobData.Uri}{sasBlobToken}";

            return new ResourceFile(blobSasUri, blobName);
        }

        /// <summary>
        /// Creates a container with the specified name in Blob storage, unless a container with that name already exists.
        /// </summary>
        /// <param name="blobClient">A <see cref="Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient"/>.</param>
        /// <param name="containerName">The name for the new container.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public async Task CreateContainerIfNotExistAsync(IAzureBlobClient blobClient, string containerName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            if (await container.CreateIfNotExistsAsync())
            {
                this.Report("Container [{0}] created.", containerName);
            }
            else
            {
                this.Report("Container [{0}] exists, skipping creation.", containerName);
            }
        }

        /// <summary>
        /// Creates a <see cref="CloudPool"/> with the specified id and configures its StartTask with the
        /// specified <see cref="ResourceFile"/> collection.
        /// </summary>
        /// <param name="poolId">The id of the <see cref="CloudPool"/> to create.</param>
        /// <param name="resourceFiles">A collection of <see cref="ResourceFile"/> objects representing blobs within
        /// a Storage account container. The StartTask will download these files from Storage prior to execution.</param>
        /// <param name="applicationReferences">If required, references to predefined applications in the batch account.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public async Task<CloudPool> CreatePoolAsync(string poolId, IList<ResourceFile> resourceFiles, IList<ApplicationPackageReference> applicationReferences = null)
        {
            this.Report("Creating pool [{0}]...", poolId);

            // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
            // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
            // If the virtualmachineconfiguration is null, this is a windows box, otherwise call it with the virtualmachine configuration
            // to create whatever box the configuration says.
            this.Pool = this.executionSettings.MachineConfig.VirtualMachineConfiguration == null
                ?
                this.BatchClient.PoolOperations.CreatePool(
                    poolId,
                    targetDedicated: this.executionSettings.MachineConfig.NumberOfNodes,
                    virtualMachineSize: this.executionSettings.MachineConfig.Size,
                    cloudServiceConfiguration: this.executionSettings.MachineConfig.CloudServiceConfiguration)
                    :
                this.BatchClient.PoolOperations.CreatePool(
                    poolId,
                    targetDedicated: this.executionSettings.MachineConfig.NumberOfNodes,
                    virtualMachineSize: this.executionSettings.MachineConfig.Size,
                    virtualMachineConfiguration: this.executionSettings.MachineConfig.VirtualMachineConfiguration);

            if (applicationReferences != null)
            {
                this.Pool.ApplicationPackageReferences = applicationReferences;
            }

            // Create and assign the StartTask that will be executed when compute nodes join the pool.
            // In this case, we copy the StartTask's resource files (that will be automatically downloaded
            // to the node by the StartTask) into the shared directory that all tasks will have access to.
            this.Pool.StartTask = new StartTask
            {
                // Specify a command line for the StartTask that copies the task application files to the
                // node's shared directory. Every compute node in a Batch pool is configured with a number
                // of pre-defined environment variables that can be referenced by commands or applications
                // run by tasks.

                // Since a successful execution of robocopy can return a non-zero exit code (e.g. 1 when one or
                // more files were successfully copied) we need to manually exit with a 0 for Batch to recognize
                // StartTask execution success.
                CommandLine = new ConsoleCommandHelper().WrapConsoleCommand($"{this.executionSettings.StartupConsoleCommand.BaseCommand} {string.Join(" ", this.executionSettings.StartupConsoleCommand.Arguments)}", this.executionSettings.MachineConfig.OperatingSystemFamily),
                ResourceFiles = resourceFiles,
                WaitForSuccess = true,
                UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin))
            };
            try
            {
                await this.Pool.CommitAsync();
            }
            catch (Exception ex)
            {
                this.Report($"Could not create pool: {ex.Message}");
                throw;
            }

            return this.Pool;
        }

        /// <summary>
        /// Creates a job in the specified pool.
        /// </summary>
        /// <param name="jobId">The id of the job to be created.</param>
        /// <param name="poolId">The id of the <see cref="CloudPool"/> in which to create the job.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public async Task<CloudJob> CreateJobAsync(string jobId, string poolId)
        {
            this.Report("Creating job [{0}]...", jobId);

            CloudJob job = this.BatchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };

            await job.CommitAsync();

            return job;
        }

        /// <summary>
        /// Creates tasks to process each of the specified input files, and submits them to the
        /// specified job for execution.
        /// </summary>
        /// <param name="jobId">The id of the job to which the tasks should be added.</param>
        /// <param name="inputFiles">A collection of <see cref="ResourceFile"/> objects representing the input files to be
        /// processed by the tasks executed on the compute nodes.</param>
        /// <param name="packageReferences">References, if needed, to applications defined in the batch account.</param>
        /// <returns>A collection of the submitted tasks.</returns>
        public async Task<List<CloudTask>> AddTasksAsync(string jobId, List<ResourceFile> inputFiles, IList<ApplicationPackageReference> packageReferences = null)
        {
            this.Report("Adding {0} tasks to job [{1}]...", inputFiles.Count, jobId);

            // Create a collection to hold the tasks that we'll be adding to the job
            List<CloudTask> tasks = new List<CloudTask>();

            if (this.executionSettings.ListEnvironmentVariables)
            {
                tasks.Add(this.GetListEnvironmentVariablesTask());
            }

            var consoleHelper = new ConsoleCommandHelper();

            // Create each of the tasks. Because we copied the task application to the
            // node's shared directory with the pool's StartTask, we can access it via
            // the shared directory on whichever node each task will run.
            foreach (ResourceFile inputFile in inputFiles)
            {
                string taskId = "task_" + inputFiles.IndexOf(inputFile);

                string taskCommandLine =
                    consoleHelper.ConstructCommand(
                            this.executionSettings.TaskConsoleCommands,
                            inputFile,
                            this.executionSettings.MachineConfig.OperatingSystemFamily);

                var task = new CloudTask(taskId, taskCommandLine);

                if (packageReferences != null)
                {
                    task.ApplicationPackageReferences = packageReferences;
                }

                task.ResourceFiles = new List<ResourceFile> { inputFile };
                tasks.Add(task);
            }

            // Add the tasks as a collection opposed to a separate AddTask call for each. Bulk task submission
            // helps to ensure efficient underlying API calls to the Batch service.
            await this.BatchClient.JobOperations.AddTaskAsync(jobId, tasks);

            return tasks;
        }

        /// <summary>
        /// Monitor the job with jobId until it either completes or the timeout period is reached.
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public bool MonitorAll(string jobId)
        {
            var monitor = new AzureBatchTaskMonitor(this.BatchClient, jobId, this.ProgressDelegate, this.executionSettings.ReportStatusFormat);

            try
            {
                monitor.StartMonitoring();
            }
            catch (Exception ex)
            {
                this.Report("An error occurred while monitoring the job with id {0}, exception: {1}", jobId, jobId, ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Monitors the specified tasks for completion and returns a value indicating whether all tasks completed successfully
        /// within the timeout period.
        /// </summary>
        /// <param name="jobId">The id of the job containing the tasks that should be monitored.</param>
        /// <param name="timeout">The period of time to wait for the tasks to reach the completed state.</param>
        /// <returns><c>true</c> if all tasks in the specified job completed with an exit code of 0 within the specified timeout period, otherwise <c>false</c>.</returns>
        public async Task<bool> MonitorTasks(string jobId, TimeSpan timeout)
        {
            bool allTasksSuccessful = true;
            const string SuccessMessage = "All tasks reached state Completed.";
            const string FailureMessage = "One or more tasks failed to reach the Completed state within the timeout period.";

            // Obtain the collection of tasks currently managed by the job. Note that we use a detail level to
            // specify that only the "id" property of each task should be populated. Using a detail level for
            // all list operations helps to lower response time from the Batch service.
            ODATADetailLevel detail = new ODATADetailLevel(selectClause: "id");
            List<CloudTask> tasks = await this.BatchClient.JobOperations.ListTasks(this.executionSettings.BatchPoolSetup.JobId, detail).ToListAsync();

            this.Report("Awaiting task completion, timeout in {0}...", timeout.ToString());

            // We use a TaskStateMonitor to monitor the state of our tasks. In this case, we will wait for all tasks to
            // reach the Completed state.
            TaskStateMonitor taskStateMonitor = this.BatchClient.Utilities.CreateTaskStateMonitor();

            try
            {
                await taskStateMonitor.WhenAll(tasks, TaskState.Active, timeout);
                this.Report("All tasks reached the 'Active' state");
            }
            catch (TimeoutException)
            {
                await this.BatchClient.JobOperations.TerminateJobAsync(jobId, FailureMessage);
                this.Report(FailureMessage);
                return false;
            }

            try
            {
                await taskStateMonitor.WhenAll(tasks, TaskState.Completed, timeout);
            }
            catch (TimeoutException)
            {
                await this.BatchClient.JobOperations.TerminateJobAsync(jobId, FailureMessage);
                this.Report(FailureMessage);
                return false;
            }

            await this.BatchClient.JobOperations.TerminateJobAsync(jobId, SuccessMessage);

            // All tasks have reached the "Completed" state, however, this does not guarantee all tasks completed successfully.
            // Here we further check each task's ExecutionInfo property to ensure that it did not encounter a scheduling error
            // or return a non-zero exit code.

            // Update the detail level to populate only the task id and executionInfo properties.
            // We refresh the tasks below, and need only this information for each task.
            detail.SelectClause = "id, executionInfo";

            foreach (CloudTask task in tasks)
            {
                // Populate the task's properties with the latest info from the Batch service
                await task.RefreshAsync(detail);

                if (task.ExecutionInformation.Result == TaskExecutionResult.Failure)
                {
                    // A scheduling error indicates a problem starting the task on the node. It is important to note that
                    // the task's state can be "Completed," yet still have encountered a scheduling error.
                    allTasksSuccessful = false;

                    this.Report("WARNING: Task [{0}] encountered a scheduling error: {1}", task.Id, task.ExecutionInformation.FailureInformation.Message);
                }
                else if (task.ExecutionInformation.ExitCode != 0)
                {
                    // A non-zero exit code may indicate that the application executed by the task encountered an error
                    // during execution. As not every application returns non-zero on failure by default (e.g. robocopy),
                    // your implementation of error checking may differ from this example.
                    allTasksSuccessful = false;

                    this.Report("WARNING: Task [{0}] returned a non-zero exit code - this may indicate task execution or completion failure.", task.Id);
                }
            }

            if (allTasksSuccessful)
            {
                this.Report("Success! All tasks completed successfully within the specified timeout period.");
            }

            return allTasksSuccessful;
        }

        /// <summary>
        /// Get a list of all compute nodes in the pool.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ComputeNode>> GetComputeNodes()
        {
            var retVal = new List<ComputeNode>();

            if (this.Pool == null)
            {
                return new List<ComputeNode>();
            }

            try
            {
                await this.Pool.RefreshAsync();

                // Get the list of pool compute nodess - can't use this because ComputeNode does not support RecentTasks property yet
                DetailLevel detailLevel = new ODATADetailLevel()
                {
                    SelectClause = "recentTasks,state,id"
                };

                IPagedEnumerable<ComputeNode> computeNodeEnumerableAsync = this.Pool.ListComputeNodes(detailLevel);
                retVal = await computeNodeEnumerableAsync.ToListAsync();
            }
            catch (Exception ex)
            {
                this.Report("Could not get compute nodes, unbound...");
            }

            return retVal;
        }

        /// <summary>
        /// Downloads all files from the specified blob storage container to the specified directory.
        /// </summary>
        /// <param name="blobClient">A <see cref="IAzureBlobClient"/>.</param>
        /// <param name="containerName">The name of the blob storage container containing the files to download.</param>
        /// <param name="directoryPath">The full path of the local directory to which the files should be downloaded.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public async Task DownloadBlobsFromContainerAsync(IAzureBlobClient blobClient, string containerName, string directoryPath)
        {
            this.Report("Downloading all files from container [{0}]...", containerName);

            // Retrieve a reference to a previously created container
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Get a flat listing of all the block blobs in the specified container
            foreach (IListBlobItem item in container.ListBlobs(null, true))
            {
                // Retrieve reference to the current blob
                CloudBlob blob = (CloudBlob)item;

                // Save blob contents to a file in the specified folder
                string localOutputFile = Path.Combine(directoryPath, blob.Name);
                await blob.DownloadToFileAsync(localOutputFile, FileMode.Create);
            }

            this.Report("All files downloaded to {0}", directoryPath);
        }

        /// <summary>
        /// Deletes the container with the specified name from Blob storage, unless a container with that name does not exist.
        /// </summary>
        /// <param name="blobClient">A <see cref="Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient"/>.</param>
        /// <param name="containerName">The name of the container to delete.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public async Task DeleteContainerAsync(IAzureBlobClient blobClient, string containerName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            try
            {
                if (container.DeleteIfExists())
                {
                    this.Report("Container [{0}] deleted.", containerName);
                }
                else
                {
                    this.Report("Container [{0}] does not exist, skipping deletion.", containerName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when deleting container {containerName}: ex: {ex}");
                throw;
            }
        }

        public void Dispose()
        {
            this.BatchClient.Dispose();
        }

        /// <summary>
        /// Callback progress on the progress delegate, if there is one.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="args"></param>
        internal void Report(string progress, params object[] args)
        {
            try
            {
                this.ProgressDelegate?.Invoke(string.Format(progress, args));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Could not report to progress delegate, reverting to console:", ex);
                Console.Error.WriteLine(progress, args);
            }
        }

        internal CloudTask GetListEnvironmentVariablesTask()
        {
            string command = this.executionSettings.MachineConfig.IsLinux() ? "printenv" : "cmd /c set";

            CloudTask listEnvVarTask = new CloudTask("ListEnvVar", command);
            return listEnvVarTask;
        }
    }
}