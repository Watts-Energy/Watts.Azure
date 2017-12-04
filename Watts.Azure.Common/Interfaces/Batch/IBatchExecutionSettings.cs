namespace Watts.Azure.Common.Interfaces.Batch
{
    using System.Collections.Generic;
    using Common.Batch.Objects;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;

    /// <summary>
    /// Settings for an Azure Batch execution.
    /// </summary>
    public interface IBatchExecutionSettings
    {
        List<BatchExecutableInfo> ExecutableInfos { get; set; }

        BatchConsoleCommand StartupConsoleCommand { get; set; }

        List<BatchConsoleCommand> TaskConsoleCommands { get; set; }

        BatchAccountSettings BatchAccountSettings { get; set; }

        BatchPoolSetup BatchPoolSetup { get; set; }

        StorageAccountSettings BatchStorageAccountSettings { get; set; }

        AzureMachineConfig MachineConfig { get; set; }

        bool CleanupAfterwards { get; set; }

        /// <summary>
        /// Connection string to the storage account associated with the batch account.
        /// </summary>
        string StorageConnectionString { get; }

        BatchSharedKeyCredentials BatchCredentials { get; }

        IList<ApplicationPackageReference> Applications { get; set; }

        /// <summary>
        /// Returns true if the settings are valid and false otherwise.
        /// </summary>
        bool IsValid { get; }

        bool ListEnvironmentVariables { get; set; }

        int TimeoutInMinutes { get; set; }

        BatchOutputContainer OutputContainer { get; set; }

        bool SaveStatistics { get; set; }

        string RedirectOutputToFileName { get; set; }

        ReportPoolStatusFormat ReportStatusFormat { get; set; }
    }
}