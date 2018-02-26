namespace Watts.Azure.Utils.Helpers.Batch
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Common.Batch;
    using Common.Batch.Jobs;
    using Common.Batch.Objects;
    using Common.Interfaces.Batch;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch creation which is meant to run an executable.
    /// </summary>
    public class ExecutableBatchCreation : BatchCreationWithInputPreparation, IBatchExecution
    {
        private BatchExecutableInfo executableInfo = new BatchExecutableInfo()
        {
            BatchExecutableContainerName = $"application-{Guid.NewGuid()}",
            BatchInputContainerName = $"input-{Guid.NewGuid()}"
        };

        public ExecutableBatchCreation(BatchCreationWithInputPreparation parent, string executableFilePath)
            : base(parent)
        {
            this.ExecutableFilePath = executableFilePath;
            this.TimeoutInMinutes = parent.TimeoutInMinutes;
            this.ExecuteTaskCommands = new List<BatchConsoleCommand>
            {
                BatchCommand.GetRunExecutableOnInputFileWithArgumentComand(Path.GetFileName(this.ExecutableFilePath), string.Empty)
            };
        }

        public ExecutableBatchCreation(ExecutableBatchCreation parent)
            : base(parent)
        {
            this.ExecutableFilePath = parent.ExecutableFilePath;
            this.TimeoutInMinutes = parent.TimeoutInMinutes;

            this.ExecuteTaskCommands.Add(
                BatchCommand.GetRunExecutableOnInputFileWithArgumentComand(Path.GetFileName(this.ExecutableFilePath), string.Empty));
        }

        public IBatchAccount BatchAccount => new BatchAccount(this.ExecutionSettings, this.BatchClient, this.ProgressReportDelegate);

        public BatchExecutableInfo ExecutableInfo
        {
            get => this.executableInfo;
            set => this.executableInfo = value;
        }

        public IBatchExecutionSettings ExecutionSettings
        {
            get
            {
                IBatchExecutionSettings retVal = new BatchExecutionSettings(new List<BatchExecutableInfo>() { this.executableInfo }, this.StartupNodeCommand, this.ExecuteTaskCommands, this.BatchAccountSettings, this.PoolSetup, this.StorageAccountSettings, this.MachineConfig, new List<ApplicationPackageReference>(), this.CleanUpAfterExecution);

                retVal.TimeoutInMinutes = this.TimeoutInMinutes;
                retVal.SaveStatistics = this.CreateStatistics;
                retVal.OutputContainer = this.OutputContainer;
                retVal.RedirectOutputToFileName = this.RedirectOutputToFileName;
                retVal.ReportStatusFormat = this.ReportStatusFormat;

                return retVal;
            }
        }

        protected string ExecutableFilePath { get; set; }

        public ExecutableBatchCreation UsingExecutableInfo(BatchExecutableInfo execInfo)
        {
            this.executableInfo.BatchExecutableContainerName = execInfo.BatchExecutableContainerName;
            this.executableInfo.BatchInputContainerName = execInfo.BatchInputContainerName;
            return this;
        }

        /// <summary>
        /// Adds a reference to an application package (that will be copied to all nodes when the tasks are executing).
        /// Invoke it multiple times to add more package references.
        /// </summary>
        /// <param name="packageReference"></param>
        /// <returns></returns>
        public ExecutableBatchCreation WithApplicationPackageReference(ApplicationPackageReference packageReference)
        {
            this.ExecutionSettings.Applications.Add(packageReference);
            return this;
        }

        public ExecutableBatchCreation WithAdditionalScriptExecutionArgument(string arg)
        {
            this.ExecutionSettings.TaskConsoleCommands.Last().Arguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Get an object ready for batch processing of the R script. The dependencies are resolved using the IDependencyResolver passed in a previous step.
        /// All input files are copied to a blob in the batch account's associated storage account, named 'input' and all scripts and their dependencies to
        /// one named 'application'. Both are created if they do not already exist.
        /// All created cloud tasks are monitored and when the are all finished, this returns.
        /// </summary>
        /// <returns>A batch execution object ready to run the batch R script</returns>
        public BatchExecutionBase GetBatchExecution()
        {
            return new BatchExecutionBase(this.BatchAccount, this.ExecutionSettings, this.InputFilePreparation, this.DependencyResolvers, new CloudAccountFactory(), this.Log);
        }
    }
}