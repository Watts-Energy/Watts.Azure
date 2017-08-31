namespace Watts.Azure.Common.Interfaces.Storage
{
    /// <summary>
    /// Interface for an Azure Fileshare.
    /// </summary>
    public interface IFileshare
    {
        void SaveDataToFile(string localFilePath);

        void DownloadFile(string filename, string localFilePath);
    }
}