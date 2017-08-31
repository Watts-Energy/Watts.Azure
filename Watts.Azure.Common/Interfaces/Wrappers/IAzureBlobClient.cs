namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Interface for a Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient in order to be able to mock it in unit tests.
    /// </summary>
    public interface IAzureBlobClient
    {
        CloudBlobContainer GetContainerReference(string containerName);
    }
}