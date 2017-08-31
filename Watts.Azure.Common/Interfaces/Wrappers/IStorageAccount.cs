namespace Watts.Azure.Common.Interfaces.Wrappers
{
    /// <summary>
    /// Interface for a CloudStorageAccount, to make it mockable.
    /// </summary>
    public interface IStorageAccount
    {
        IAzureBlobClient CreateCloudBlobClient();
    }
}