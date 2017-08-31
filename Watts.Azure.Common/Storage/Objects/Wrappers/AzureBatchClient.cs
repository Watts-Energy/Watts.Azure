namespace Watts.Azure.Common.Storage.Objects.Wrappers
{
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;

    /// <summary>
    /// A wrapper for BatchClient in Microsoft.Azure.Batch.
    /// The wrapper is created in order to be able to mock the client in unit tests.
    /// </summary>
    public class AzureBatchClient : IAzureBatchClient
    {
        private readonly BatchClient client;

        public AzureBatchClient(BatchSharedKeyCredentials credentials)
        {
            this.client = BatchClient.Open(credentials);
        }

        public IPoolOperations PoolOperations => new PoolOperationsWrapper(this.client.PoolOperations);

        public IJobOperations JobOperations => new JobOperationsWrapper(this.client.JobOperations);

        public Utilities Utilities => this.client.Utilities;

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}