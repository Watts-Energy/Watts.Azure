namespace Watts.Azure.Utils.Helpers.Batch
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.Batch.Jobs;
    using Common.Batch.Objects;
    using Common.Interfaces.Batch;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch created to execute an R script in azure batch.
    /// </summary>
    public class RBatchCreation : BatchCreationWithInputPreparation, IBatchExecution
    {
        private int maxRScriptMemoryMb = 4000;

        public RBatchCreation(BatchCreationWithInputPreparation parent, string scriptName)
            : base(parent)
        {
            this.RScriptName = scriptName;
            this.TimeoutInMinutes = parent.TimeoutInMinutes;

            if (!this.MachineConfig.IsLinux())
            {
                this.ExecuteTaskCommands = new List<BatchConsoleCommand>()
                {
                    new BatchConsoleCommand()
                    {
                        BaseCommand = string.Format(CommandLibrary.AzureRScriptCommandWindows.Replace("@rVersion", this.RVersion).Replace("@rScriptName", this.RScriptName)),
                        Arguments = new List<string>() { }
                    }
                };
            }
            else
            {
                this.ExecuteTaskCommands = new List<BatchConsoleCommand>()
                {
                    new BatchConsoleCommand()
                    {
                        BaseCommand = string.Format(CommandLibrary.AzureRScriptCommandLinux).Replace("@rScriptName", this.RScriptName),
                        Arguments = new List<string>()
                    }
                };

                // Modify the startup node command to copy files to the shared directory, update apt and install r-base
                this.StartupNodeCommand = new BatchConsoleCommand()
                {
                    BaseCommand = CommandLibrary.LinuxNodeStartupCommandInstallR,
                    Arguments = new List<string>()
                };
            }
        }

        public RBatchCreation(RBatchCreation parent)
           : base(parent)
        {
            this.ExecuteTaskCommands = parent.ExecuteTaskCommands;
            this.TimeoutInMinutes = parent.TimeoutInMinutes;
            this.RScriptName = parent.RScriptName;
            this.RVersion = parent.RVersion;
        }

        public IBatchAccount BatchAccount => new BatchAccount(this.ExecutionSettings, this.BatchClient, this.ProgressReportDelegate);

        public string RScriptName { get; set; }

        public string RVersion { get; set; } = Globals.DefaultRVersion;

        public IBatchExecutionSettings ExecutionSettings
        {
            get
            {
                System.Guid randomGuid = System.Guid.NewGuid();
                var executableInfos = new List<BatchExecutableInfo>()
                {
                        new BatchExecutableInfo()
                        {
                            BatchExecutableContainerName = $"application-{randomGuid}",
                            BatchInputContainerName = $"input-{randomGuid}"
                        }
                    };

                // If windows, add an application package reference to R, otherwise apt-get will have installed r-base so there's no need.
                var applicationReferences = this.MachineConfig.IsLinux() ? null
                    :
                    new List<ApplicationPackageReference>()
                {
                    new ApplicationPackageReference()
                    {
                        ApplicationId = "R",
                        Version = this.RVersion
                    }
                };

                IBatchExecutionSettings retVal = new BatchExecutionSettings(executableInfos, this.StartupNodeCommand, this.ExecuteTaskCommands, this.BatchAccountSettings, this.PoolSetup, this.StorageAccountSettings, this.MachineConfig, applicationReferences, this.CleanUpAfterExecution);

                retVal.TimeoutInMinutes = this.TimeoutInMinutes;
                retVal.SaveStatistics = this.CreateStatistics;
                retVal.ShouldDownloadOutput = this.ShouldDownloadOutput;
                retVal.ReportStatusFormat = this.ReportStatusFormat;

                return retVal;
            }
        }

        private int MaxRScriptMemoryMb
        {
            get => this.maxRScriptMemoryMb;

            set
            {
                this.ExecuteTaskCommands.First().BaseCommand = this.ExecuteTaskCommands.First().BaseCommand.Replace($"--max-mem-size={this.maxRScriptMemoryMb}M", $"--max-mem-size={value}M");
                this.maxRScriptMemoryMb = value;
            }
        }

        public RBatchCreation UseRVersion(string version)
        {
            this.RVersion = version;
            return this;
        }

        public RBatchCreation SetMaxMemoryForRScript(int megaBytes)
        {
            this.MaxRScriptMemoryMb = megaBytes;
            return this;
        }

        /// <summary>
        /// Adds a reference to an application package (that will be copied to all nodes when the tasks are executing).
        /// Invoke it multiple times to add more package references.
        /// </summary>
        /// <param name="packageReference"></param>
        /// <returns></returns>
        public RBatchCreation WithApplicationPackageReference(ApplicationPackageReference packageReference)
        {
            this.ExecutionSettings.Applications.Add(packageReference);
            return this;
        }

        public RBatchCreation WithAdditionalScriptExecutionArgument(string arg)
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