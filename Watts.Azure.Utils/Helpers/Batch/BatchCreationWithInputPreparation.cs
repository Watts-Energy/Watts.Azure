namespace Watts.Azure.Utils.Helpers.Batch
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Common;
    using Common.Batch.Objects;
    using Common.General;
    using Common.Interfaces.General;
    using Common.Interfaces.Wrappers;
    using Common.Storage.Objects.Wrappers;
    using Watts.Azure.Utils.Interfaces.Batch;

    public class BatchCreationWithInputPreparation : BatchCreationWithPoolSetup, IBatchCreationWithInputPreparation
    {
        public BatchCreationWithInputPreparation(BatchCreationWithPoolSetup parent, IPrepareInputFiles inputFilePreparation)
            : base(parent)
        {
            this.InputFilePreparation = inputFilePreparation;
        }

        public BatchCreationWithInputPreparation(BatchCreationWithInputPreparation parent)
            : base(parent)
        {
            this.InputFilePreparation = parent.InputFilePreparation;
            this.CreateStatistics = parent.CreateStatistics;
            this.Log = parent.Log;
            this.OutputContainer = parent.OutputContainer;
            this.ProgressReportDelegate = parent.ProgressReportDelegate;
            this.ReportStatusFormat = parent.ReportStatusFormat;
        }

        /// <summary>
        /// Gets or sets the timeout for the batch, in minutes. Defaults to 30 minutes.
        /// </summary>
        public int TimeoutInMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether statistics for the batch run should be saved
        /// </summary>
        public bool CreateStatistics { get; set; } = false;

        /// <summary>
        /// The format in which status should be reported while a batch is executing.
        /// </summary>
        public ReportPoolStatusFormat ReportStatusFormat { get; set; }

        /// <summary>
        /// A progress delegate that is used to report progress on.
        /// </summary>
        protected Action<string> ProgressReportDelegate { get; set; }

        protected bool CleanUpAfterExecution { get; set; } = true;

        protected string RedirectOutputToFileName { get; set; }

        /// <summary>
        /// The log to log errors and debug information into.
        /// </summary>
        protected ILog Log { get; set; }

        protected BatchOutputContainer OutputContainer { get; set; }

        /// <summary>
        /// An instance of a class that prepares input files for tasks to be executed in Azure batch.
        /// </summary>
        protected IPrepareInputFiles InputFilePreparation { get; set; }

        /// <summary>
        /// A batch client used to communicate with Azure batch.
        /// </summary>
        protected IAzureBatchClient BatchClient => new AzureBatchClient(this.Credentials);

        /// <summary>
        /// Specify that the batch should report its progress using the given progress delegate.
        /// </summary>
        /// <param name="progressDelegate"></param>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation ReportingProgressUsing(
            Action<string> progressDelegate)
        {
            this.ProgressReportDelegate = progressDelegate;
            return this;
        }

        /// <summary>
        /// Specify that the batch execution should log to the console.
        /// </summary>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation ReportProgressToConsole()
        {
            this.ProgressReportDelegate = Console.WriteLine;
            return this;
        }

        public IBatchCreationWithInputPreparation ReportStatusInFormat(ReportPoolStatusFormat statusFormat)
        {
            this.ReportStatusFormat = statusFormat;
            return this;
        }

        public IBatchCreationWithInputPreparation UploadOutputTo(BatchOutputContainer outputContainer)
        {
            this.OutputContainer = outputContainer;
            return this;
        }

        public IBatchCreationWithInputPreparation RedirectOutputToFile(string filename)
        {
            this.RedirectOutputToFileName = filename;
            return this;
        }

        /// <summary>
        /// Specify the log to log to. Is null if not specified.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation LogTo(ILog log)
        {
            this.Log = log;
            return this;
        }

        /// <summary>
        /// Specify that the batch should save statistics about how long it took to execute, once finished.
        /// </summary>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation SaveStatistics()
        {
            this.CreateStatistics = true;
            return this;
        }

        /// <summary>
        /// Specify that the batch should skip saving statistics about how long it took to execute, once finished.
        /// </summary>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation DontSaveStatistics()
        {
            this.CreateStatistics = false;
            return this;
        }

        /// <summary>
        /// Sets the timeout in minutes for the batch execution. Note that the execution will abort if this time limit is exceeded.
        /// If not set explicitly it defaults to 30 minutes.
        /// </summary>
        /// <param name="minutes">The number of minutes before a timeout.</param>
        /// <returns></returns>
        public IBatchCreationWithInputPreparation SetTimeoutInMinutes(int minutes)
        {
            this.TimeoutInMinutes = minutes;
            return this;
        }

        public void CleanUpAfter()
        {
            this.CleanUpAfterExecution = true;
        }

        public void DoNotCleanUpAfter()
        {
            this.CleanUpAfterExecution = false;
        }

        /// <summary>
        /// Execute the rscript with the given name.
        /// NOTE this should NOT be the full name, just the name of the script.
        /// This is used when building the command to run the R script on the node.
        /// </summary>
        /// <param name="scriptFileName">The NAME of the r script to run, e.g. 'main.R'</param>
        /// <returns></returns>
        public RBatchCreation ExecuteRScript(string scriptFileName)
        {
            return new RBatchCreation(this, scriptFileName);
        }

        /// <summary>
        /// Specify the R code to execute as a string.
        /// Note that this is saved locally first, then uplodaed to the application container in batch.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public RBatchCreation ExecuteRCode(string[] code)
        {
            string tempFileName = $"{Guid.NewGuid()}_main.R";
            File.WriteAllLines(tempFileName, code);
            var manualDependencyResolver = DependencyResolver.UsingFunction(() => new List<string>());
            manualDependencyResolver.AddFileDependency(tempFileName);

            this.DependencyResolvers.Add(manualDependencyResolver);

            return this.ExecuteRScript(tempFileName);
        }

        /// <summary>
        /// Specify the executable to run on the nodes.
        /// IMPORTANT: This step sets the console command to run on nodes. If you need to specify arguments to the invocation,
        /// do so on the object returned by this method.
        /// </summary>
        /// <param name="executableFilePath">The FULL PATH to the executable, e.g. 'C:\\Development\\MyProgram\bin\Debug\MyProgram.exe'</param>
        /// <returns></returns>
        public ExecutableBatchCreation RunExecutable(string executableFilePath)
        {
            return new ExecutableBatchCreation(this, executableFilePath);
        }
    }
}