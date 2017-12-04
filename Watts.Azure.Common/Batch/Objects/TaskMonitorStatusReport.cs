namespace Watts.Azure.Common.Batch.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// Class responsible for reporting the progress of a batch job in various formats.
    /// </summary>
    public class TaskMonitorStatusReport
    {
        private readonly Action<string> progressDelegate;
        private readonly ReportPoolStatusFormat format;
        private DateTime startTime;

        public TaskMonitorStatusReport(Action<string> progressDelegate, DateTime startTime, ReportPoolStatusFormat format)
        {
            this.progressDelegate = progressDelegate;
            this.startTime = startTime;
            this.format = format;
        }

        public DateTime StartTime
        {
            get => this.startTime;
            set => this.startTime = value;
        }

        public void ReportStatus(List<CloudTask> tasks)
        {
            switch (this.format)
            {
                case ReportPoolStatusFormat.FlatList:
                    this.ReportFlatList(tasks);
                    break;

                case ReportPoolStatusFormat.GroupedByState:
                    this.ReportGroupedByState(tasks);
                    break;

                case ReportPoolStatusFormat.Summary:
                    this.ReportSummary(tasks);
                    break;

                default:
                    break;
            }
        }

        internal void ReportFlatList(List<CloudTask> tasks)
        {
            string tabSeparator = "\t\t\t";

            this.Report(string.Empty);
            this.Report("################");
            this.Report("################");
            this.Report($"Update state... ({DateTime.Now})");
            this.Report($"Elapsed time: {DateTime.Now - this.startTime}");
            this.Report($"Id{tabSeparator}DisplayName{tabSeparator}State{tabSeparator}ExitCode");
            this.Report("----------------");
            tasks.ForEach(t => this.Report($"Task {t.Id}{tabSeparator}{t.DisplayName}{tabSeparator}{t.State}{tabSeparator}{this.GetExitCodeDescription(t.ExecutionInformation)}"));
            this.Report("----------------");
            this.Report("################");
            this.Report("################");
            this.Report(string.Empty);
        }

        internal void ReportGroupedByState(List<CloudTask> tasks)
        {
            string tabSeparator = "\t\t\t";

            var groupedByState = tasks.GroupBy(p => p.State).OrderBy(t => t.First().State);

            this.Report(string.Empty);
            this.Report("################");
            this.Report("################");
            this.Report($"Update state... ({DateTime.Now})");
            this.Report($"Elapsed time: {(DateTime.Now - this.startTime).ToPrettyFormat()}");
            this.Report($"Id{tabSeparator}DisplayName{tabSeparator}ExitCode");

            groupedByState.ToList().ForEach(p =>
            {
                this.Report(string.Empty);
                this.Report($"STATE: {p.First().State} ({p.Count()})");

                foreach (var task in p)
                {
                    this.Report($"{task.Id}{tabSeparator}{task.DisplayName}{tabSeparator}{this.GetExitCodeDescription(task.ExecutionInformation)}");
                }

                this.Report(string.Empty);
            });

            this.Report("################");
            this.Report("################");
            this.Report(string.Empty);
        }

        internal string GetExitCodeDescription(TaskExecutionInformation executionInformation)
        {
            if (executionInformation?.ExitCode == null)
            {
                return "Not finished";
            }

            if (executionInformation.ExitCode == 1)
            {
                return "Encountered errors";
            }
            else if (executionInformation.ExitCode == 0)
            {
                return "Successfully finished";
            }

            return executionInformation?.ToString();
        }

        internal void ReportSummary(List<CloudTask> tasks)
        {
            string tabSeparator = "\t\t\t";
            this.Report(string.Empty);
            this.Report("################");
            this.Report("################");
            this.Report($"Update state... ({DateTime.Now})");
            this.Report($"Elapsed time: {DateTime.Now - this.startTime}");
            var groupedByState = tasks.GroupBy(p => p.State).OrderBy(t => t.First().State);

            groupedByState.ToList().ForEach(p =>
            {
                this.Report($"{p.First().State}:{tabSeparator}{p.Count()} nodes");
            });

            this.Report("################");
            this.Report("################");
            this.Report(string.Empty);
        }

        internal void Report(string progress, params object[] args)
        {
            this.progressDelegate?.Invoke(string.Format(progress, args));
        }
    }
}