namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// An interface for Microsoft.Azure.Batch.JobOperations in order to make it mockable in unit tests.
    /// </summary>
    public interface IJobOperations
    {
        Task DeleteJobAsync(string jobId);

        CloudJob CreateJob();

        Task AddTaskAsync(string jobId, List<CloudTask> tasks);

        IPagedEnumerable<CloudTask> ListTasks(string jobId, DetailLevel detailLevel = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null);

        Task TerminateJobAsync(string jobId, string terminateReason = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}