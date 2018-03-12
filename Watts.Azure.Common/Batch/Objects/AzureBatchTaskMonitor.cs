namespace Watts.Azure.Common.Batch.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Monitors Azure Batch tasks and reports their progress.
    /// </summary>
    public class AzureBatchTaskMonitor
    {
        private readonly Action<string> progressDelegate;
        private readonly IAzureBatchClient client;
        private readonly string jobId;
        private readonly ODATADetailLevel jobDetailLevel = new ODATADetailLevel(selectClause: "id,state,executionInfo");
        private readonly ReportPoolStatusFormat format;
        private readonly TaskMonitorStatusReport statusReporter;

        private TaskState currentState = TaskState.Preparing;
        private Timer checkTasksTimer;
        private DateTime startTime;

        public AzureBatchTaskMonitor(IAzureBatchClient client, string jobId, Action<string> progressDelegate, ReportPoolStatusFormat format = ReportPoolStatusFormat.FlatList)
        {
            this.client = client;
            this.jobId = jobId;
            this.progressDelegate = progressDelegate;
            this.format = format;

            this.statusReporter = new TaskMonitorStatusReport(this.progressDelegate, this.startTime, this.format);
        }

        /// <summary>
        /// The number of seconds between status checks. Defaults to 30 seconds.
        /// </summary>
        public int SecondsBetweenChecks { get; set; } = 30;

        public AutoResetEvent FinishedMutex { get; set; } = new AutoResetEvent(false);

        /// <summary>
        /// Start monitoring the job.
        /// </summary>
        /// <returns></returns>
        public void StartMonitoring()
        {
            int secondsToMs = 1000;

            this.startTime = DateTime.Now;
            this.statusReporter.StartTime = this.startTime;
            this.checkTasksTimer = new Timer(this.SecondsBetweenChecks * secondsToMs);
            this.checkTasksTimer.Elapsed += this.CheckTasksTimer_Elapsed;
            this.checkTasksTimer.Start();
        }

        public void CheckJob()
        {
            this.Report("Checking status. Time is {0}", DateTime.Now);

            Task.Run(this.CheckCurrentTaskStatesAsync);
        }

        /// <summary>
        /// Get the task states and report to the console.
        /// </summary>
        /// <returns></returns>
        internal async Task<List<CloudTask>> CheckCurrentTaskStatesAsync()
        {
            var tasks = await this.client.JobOperations.ListTasks(this.jobId, this.jobDetailLevel).ToListAsync();

            if (this.format != ReportPoolStatusFormat.Silent)
            {
                this.statusReporter.ReportStatus(tasks);
            }

            var taskMinState = tasks.Min(p => p.State);

            // If all tasks have moved to the next state, report it to Console.
            if (taskMinState > this.currentState)
            {
                Console.WriteLine($"All tasks reached state {taskMinState}");
                this.currentState = taskMinState.Value;
            }

            return tasks;
        }

        internal void StopTimer()
        {
            this.checkTasksTimer.Stop();
            this.checkTasksTimer.Dispose();
        }

        internal void CheckTasksTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.CheckJob();

            // If all tasks have reached the completed state, stop monitoring.
            if (this.currentState == TaskState.Completed)
            {
                this.StopTimer();
                this.FinishedMutex.Set();
            }
        }

        internal void Report(string progress, params object[] args)
        {
            this.progressDelegate?.Invoke(string.Format(progress, args));
        }
    }
}