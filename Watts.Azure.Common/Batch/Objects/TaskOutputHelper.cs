namespace Watts.Azure.Common.Batch.Objects
{
    using System;
    using System.Threading.Tasks;
    using Interfaces.Batch;
    using Microsoft.Azure.Batch;

    public class TaskOutputHelper
    {
        private readonly IBatchAccount account;

        public TaskOutputHelper(IBatchAccount account)
        {
            this.account = account;
        }

        public async Task<string[]> GetStdOut(CloudJob job, CloudTask task)
        {
            var stdOutFile = await this.account.BatchClient.JobOperations.GetStdOut(job.Id, task.Id);
            string stdOut = await stdOutFile.ReadAsStringAsync();

            return stdOut.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public async Task<string[]> GetStdErr(CloudJob job, CloudTask task)
        {
            var stdErrFile = await this.account.BatchClient.JobOperations.GetStdErr(job.Id, task.Id);
            string stdErr = await stdErrFile.ReadAsStringAsync();
            return stdErr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}