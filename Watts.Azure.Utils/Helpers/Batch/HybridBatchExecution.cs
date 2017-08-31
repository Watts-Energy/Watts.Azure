namespace Watts.Azure.Utils.Helpers.Batch
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.Batch.Jobs;
    using Common.Batch.Objects;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A multi-step batch execution, possibly combining different types, e.g. R-script and executable.
    /// </summary>
    public class HybridBatchExecution
    {
        private BatchExecutionBase overallExecution;

        private HybridBatchExecution(IBatchExecution batch)
        {
            this.Batches.Add(batch);
        }

        public List<IBatchExecution> Batches { get; set; } = new List<IBatchExecution>();

        public static HybridBatchExecution First(IBatchExecution batch)
        {
            return new HybridBatchExecution(batch);
        }

        public HybridBatchExecution Then(IBatchExecution batch)
        {
            this.Batches.Add(batch);
            return this;
        }

        public BatchExecutionBase GetCombinedBatchExecutionBase()
        {
            // Combine the basebatch into one that combines them all.
            this.overallExecution = this.Batches.First().GetBatchExecution();

            foreach (var batch in this.Batches.Skip(1))
            {
                this.overallExecution.Settings.ExecutableInfos.AddRange(batch.ExecutionSettings.ExecutableInfos);
                this.overallExecution.Settings.TaskConsoleCommands.AddRange(batch.ExecutionSettings.TaskConsoleCommands);
                this.overallExecution.DependencyResolvers.AddRange(batch.DependencyResolvers);
            }

            return this.overallExecution;
        }

        public List<TaskOutput> GetOutput()
        {
            return this.overallExecution.GetExecutionOutput();
        }
    }
}