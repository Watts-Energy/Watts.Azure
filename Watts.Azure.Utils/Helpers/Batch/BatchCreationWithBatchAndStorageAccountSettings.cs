namespace Watts.Azure.Utils.Helpers.Batch
{
    using System.Collections.Generic;
    using Common.Batch;
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Common.Storage.Objects;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch creation with both batch account and storage account settings specified.
    /// </summary>
    public class BatchCreationWithBatchAndStorageAccountSettings : BatchCreationWithBatchAccountInfo, IBatchCreationWithBatchAndStorageAccountSettings
    {
        public BatchCreationWithBatchAndStorageAccountSettings(BatchCreationWithBatchAndStorageAccountSettings parent) : base(parent)
        {
            this.StorageAccountSettings = parent?.StorageAccountSettings;

            this.StartupNodeCommand = parent?.StartupNodeCommand;
            this.ExecuteTaskCommands = parent?.ExecuteTaskCommands;
        }

        public BatchCreationWithBatchAndStorageAccountSettings(BatchCreationWithBatchAccountInfo parent, StorageAccountSettings storageAccountSettings) : base(parent)
        {
            this.StorageAccountSettings = storageAccountSettings;

            this.StartupNodeCommand = BatchCommand.GetCopyTaskApplicationFilesToNodeSharedDirectory();
        }

        /// <summary>
        /// Batch storage account settings.
        /// </summary>
        public StorageAccountSettings StorageAccountSettings { get; set; }

        /// <summary>
        /// A command to be run when a node enters the pool.
        /// </summary>
        public BatchConsoleCommand StartupNodeCommand { get; set; }

        /// <summary>
        /// The commands to execute the batch.
        /// </summary>
        public List<BatchConsoleCommand> ExecuteTaskCommands { get; set; }

        /// <summary>
        /// OPTIONAL
        /// Run the specified command on all nodes as they join the cluster.
        /// If not specified, the default is to copy all input and executable files into the shared directory (see BatchCommand.CopyTaskApplicationFilesToNodeSharedDirectory())
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public IBatchCreationWithBatchAndStorageAccountSettings RunStartupCommandOnAllNodes(BatchConsoleCommand command)
        {
            this.StartupNodeCommand = command;
            return this;
        }

        /// <summary>
        /// Set an object to resolve dependencies, that returns a list of files to be uploaded to the 'application' blob container.
        /// If there are no dependencies other than the script itself, use NoDependencies().
        /// </summary>
        /// <param name="dependencyResolver"></param>
        /// <returns></returns>
        public IBatchCreationWithDependencyResolver ResolveDependenciesUsing(IBatchDependencyResolver dependencyResolver)
        {
            return new BatchCreationWithDependencyResolver(this, dependencyResolver);
        }

        /// <summary>
        /// Give a list of dependency resolvers that each return a list of files that must be present in order for the batch
        /// to run.
        /// </summary>
        /// <param name="dependencyResolvers"></param>
        /// <returns></returns>
        public IBatchCreationWithDependencyResolver ResolveDependenciesUsing(List<IBatchDependencyResolver> dependencyResolvers)
        {
            return new BatchCreationWithDependencyResolver(this, dependencyResolvers);
        }

        public IBatchCreationWithDependencyResolver NoDependencies()
        {
            return new BatchCreationWithDependencyResolver(this, dependencyResolver: null);
        }
    }
}