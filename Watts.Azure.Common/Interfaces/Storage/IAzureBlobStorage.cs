namespace Watts.Azure.Common.Interfaces.Storage
{
    /// <summary>
    /// Interface for a Azure blob storage
    /// </summary>
    public interface IAzureBlobStorage
    {
        void UploadFromFile(string fileName, string blobName);

        string GetBlobContents(string blobName);
    }
}