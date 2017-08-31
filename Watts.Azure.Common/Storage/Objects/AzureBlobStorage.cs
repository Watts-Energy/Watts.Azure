namespace Watts.Azure.Common.Storage.Objects
{
    using Interfaces.Storage;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// An Azure blob providing methods to upload blobs and to get their contents.
    /// </summary>
    public class AzureBlobStorage : IAzureBlobStorage
    {
        private readonly CloudBlobClient blobClient;
        private readonly string blobContainerName;

        private AzureBlobStorage(CloudBlobClient client, string blobContainerName)
        {
            this.blobClient = client;
            this.blobContainerName = blobContainerName;
        }

        public static AzureBlobStorage Connect(string connectionString, string blobContainerName)
        {
            if (!blobContainerName.ToLower().Equals(blobContainerName))
            {
                throw new StorageException("The container name must be all lowercase and cannot contain special characters...");
            }

            var storageAccount =
    CloudStorageAccount.Parse(connectionString);
            return new AzureBlobStorage(storageAccount.CreateCloudBlobClient(), blobContainerName);
        }

        public void UploadFromFile(string fileName, string blobName)
        {
            var container = this.blobClient.GetContainerReference(this.blobContainerName);

            container.CreateIfNotExists();

            var blob = container.GetBlockBlobReference(blobName);
            blob.UploadFromFile(fileName);
        }

        public string GetBlobContents(string blobName)
        {
            var container = this.blobClient.GetContainerReference(this.blobContainerName);

            var blobReference = container.GetBlockBlobReference(blobName);

            string blobContents = null;

            try
            {
                blobContents = blobReference?.DownloadText();
            }
            catch (StorageException exception)
            {
                return string.Empty;
            }

            return blobContents;
        }

        public void DeleteContainerIfExists()
        {
            var containerReference = this.blobClient.GetContainerReference(this.blobContainerName);

            containerReference.DeleteIfExists();
        }
    }
}