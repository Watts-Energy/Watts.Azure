namespace Watts.Azure.Common.Batch.Objects
{
    using System.Collections.Generic;
    using Interfaces.Batch;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;
    using Storage.Objects;

    public class BatchExecutionSettings : IBatchExecutionSettings
    {
        public BatchExecutionSettings(
                                     List<BatchExecutableInfo> executableInfos,
                                     BatchConsoleCommand startupNodeCommand,
                                     List<BatchConsoleCommand> executeTaskCommands,
                                     BatchAccountSettings batchAccountSettings,
                                     BatchPoolSetup poolSetup,
                                     StorageAccountSettings storageAccountSettings,
                                     AzureMachineConfig machineConfig,
                                     IList<ApplicationPackageReference> applicationReferences = null,
                                     bool cleanUpAfterwards = true,
                                     bool listEnvironmentVariables = true)
        {
            this.ExecutableInfos = executableInfos;
            this.StartupConsoleCommand = startupNodeCommand;
            this.TaskConsoleCommands = executeTaskCommands;
            this.BatchAccountSettings = batchAccountSettings;
            this.BatchPoolSetup = poolSetup;
            this.BatchStorageAccountSettings = storageAccountSettings;
            this.MachineConfig = machineConfig;
            this.Applications = applicationReferences;
            this.CleanupAfterwards = cleanUpAfterwards;
            this.ListEnvironmentVariables = listEnvironmentVariables;
        }

        public int TimeoutInMinutes { get; set; } = 30;

        /// <summary>
        /// General settings for the batch account.
        /// </summary>
        public BatchAccountSettings BatchAccountSettings { get; set; }

        /// <summary>
        /// Information about the executable to be parallelized.
        /// </summary>
        public List<BatchExecutableInfo> ExecutableInfos { get; set; }

        /// <summary>
        /// The console command that should be run on each node as they enter the pool. E.g. to copy input files to local directory, install software and things like that.
        /// </summary>
        public BatchConsoleCommand StartupConsoleCommand { get; set; }

        /// <summary>
        /// The console command that should be run on each node.
        /// </summary>
        public List<BatchConsoleCommand> TaskConsoleCommands { get; set; }

        /// <summary>
        /// Information about the storage account connected with the batch account.
        /// </summary>
        public StorageAccountSettings BatchStorageAccountSettings { get; set; }

        /// <summary>
        /// Setup for the pool
        /// </summary>
        public BatchPoolSetup BatchPoolSetup { get; set; }

        /// <summary>
        /// Definition of the machines to spin up and use for computing.
        /// </summary>
        public AzureMachineConfig MachineConfig { get; set; }

        /// <summary>
        /// The name of the container where executables are placed.
        /// </summary>
        public string BatchExecutableContainerName { get; set; }

        /// <summary>
        /// The name of the container where input files are placed.
        /// </summary>
        public string BatchInputContainerName { get; set; }

        /// <summary>
        /// The name (not path) of the batch executable.
        /// </summary>
        public string BatchExecutableName { get; set; }

        /// <summary>
        /// Indicates whether the pool and job should be removed after the processing is done.
        /// </summary>
        public bool CleanupAfterwards { get; set; }

        /// <summary>
        /// Indicates whether a statistics entity should be saved when the batch completes, storing various things related to the batch
        /// that was executed, e.g. how long it took, how many nodes were used, etc.
        /// </summary>
        public bool SaveStatistics { get; set; }

        /// <summary>
        /// The name of the output file that output should be redirected to when executing a task.
        /// If this is empty, output is not redirected.
        /// </summary>
        public string RedirectOutputToFileName { get; set; }

        /// <summary>
        /// The format in which progress should be reported while a batch is running.
        /// </summary>
        public ReportPoolStatusFormat ReportStatusFormat { get; set; } = ReportPoolStatusFormat.GroupedByState;

        /// <summary>
        /// Credentials for the batch account.
        /// </summary>
        public BatchSharedKeyCredentials BatchCredentials
           =>
           new BatchSharedKeyCredentials(
                                         this.BatchAccountSettings.BatchAccountUrl,
                                         this.BatchAccountSettings.BatchAccountName,
                                         this.BatchAccountSettings.BatchAccountKey);

        /// <summary>
        /// Get the connection string to the storage account connected with the batch account.
        /// </summary>
        public string StorageConnectionString => $"DefaultEndpointsProtocol=https;AccountName={this.BatchStorageAccountSettings.StorageAccountName};AccountKey={this.BatchStorageAccountSettings.StorageAccountKey}";

        /// <summary>
        /// Get a boolean indicating whether or not all required settings have been filled.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountKey) ||
                    string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountName) ||
                    string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountUrl) ||
                    string.IsNullOrEmpty(this.BatchStorageAccountSettings.StorageAccountKey) ||
                    string.IsNullOrEmpty(this.BatchStorageAccountSettings.StorageAccountName) ||
                    string.IsNullOrEmpty(this.BatchPoolSetup.JobId) ||
                    string.IsNullOrEmpty(this.BatchPoolSetup.PoolId))
                {
                    return false;
                }

                return true;
            }
        }

        public bool ListEnvironmentVariables { get; set; }

        public BatchOutputContainer OutputContainer { get; set; }

        public IList<ApplicationPackageReference> Applications { get; set; }
    }
}