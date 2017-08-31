namespace Watts.Azure.Common.Storage.Objects.Wrappers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// A wrapper of a Microsoft.Azure.Batch.JobOperations in order to make it mockable in unit tests.
    /// </summary>
    public class JobOperationsWrapper : IJobOperations
    {
        private readonly JobOperations jobOperations;

        public JobOperationsWrapper(JobOperations operations)
        {
            this.jobOperations = operations;
        }

        public async Task DeleteJobAsync(string jobId)
        {
            await this.jobOperations.DeleteJobAsync(jobId);
        }

        public CloudJob CreateJob()
        {
            return this.jobOperations.CreateJob();
        }

        public async Task AddTaskAsync(string jobId, List<CloudTask> tasks)
        {
            await this.jobOperations.AddTaskAsync(jobId, tasks);
        }

        public IPagedEnumerable<CloudTask> ListTasks(string jobId, DetailLevel detailLevel = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null)
        {
            return this.jobOperations.ListTasks(jobId, detailLevel, additionalBehaviors);
        }

        public async Task TerminateJobAsync(string jobId, string terminateReason = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.jobOperations.TerminateJobAsync(jobId, terminateReason, additionalBehaviors, cancellationToken);
        }
    }
}