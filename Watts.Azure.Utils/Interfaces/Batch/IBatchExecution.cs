namespace Watts.Azure.Utils.Interfaces.Batch
{
    using System.Collections.Generic;
    using Common.Batch.Jobs;
    using Common.Interfaces.Batch;
    using Common.Interfaces.General;

    /// <summary>
    /// Interface for a batch execution
    /// </summary>
    public interface IBatchExecution
    {
        IBatchExecutionSettings ExecutionSettings { get; }

        List<IBatchDependencyResolver> DependencyResolvers { get; set; }

        BatchExecutionBase GetBatchExecution();
    }
}