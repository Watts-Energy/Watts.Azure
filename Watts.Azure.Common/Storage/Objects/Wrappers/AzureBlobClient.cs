namespace Watts.Azure.Common.Storage.Objects.Wrappers
{
    using Interfaces.Wrappers;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Wrapper of a Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient in order to be able to mock it in unit tests.
    /// </summary>
    public class AzureBlobClient : IAzureBlobClient
    {
        private readonly CloudBlobClient client;

        public AzureBlobClient(CloudBlobClient client)
        {
            this.client = client;
        }

        public CloudBlobContainer GetContainerReference(string containerName)
        {
            return this.client.GetContainerReference(containerName);
        }
    }
}