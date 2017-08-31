namespace Watts.Azure.Common.Storage.Objects.Wrappers
{
    using Interfaces.Wrappers;
    using Microsoft.WindowsAzure.Storage;

    /// <summary>
    /// Wrapper for a CloudStorageAccount, to add an interface and make it mockable.
    /// </summary>
    public class StorageAccount : IStorageAccount
    {
        private readonly CloudStorageAccount cloudStorageAccount;

        private StorageAccount(string connectionString)
        {
            this.cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
        }

        public static StorageAccount Parse(string connectionString)
        {
            return new StorageAccount(connectionString);
        }

        public IAzureBlobClient CreateCloudBlobClient()
        {
            return new AzureBlobClient(this.cloudStorageAccount.CreateCloudBlobClient());
        }
    }
}