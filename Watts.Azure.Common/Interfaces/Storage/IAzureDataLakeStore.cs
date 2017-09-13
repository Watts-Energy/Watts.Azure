namespace Watts.Azure.Common.Interfaces.Storage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.DataLake.Store.Models;
    using Watts.Azure.Common.Interfaces.DataFactory;
    using Watts.Azure.Common.Interfaces.Security;

    internal interface IAzureDataLakeStore : IAzureLinkedService
    {
        IAzureActiveDirectoryAuthentication Authenticator { get; }

        string Directory { get; set; }

        Task CreateDirectory(string relativePath);

        Task DeleteDirectory(string relativePath = "", bool recursive = false);

        Task DeleteFile(string relativePath);

        bool PathExists(string relativePath);

        void UploadFile(string localSourceFilePath, string destinationRelativeFilePath, bool force = true);

        FileStatusProperties GetItemInfo(string path);

        List<FileStatusProperties> ListItems(string directoryPath);

        Task ConcatenateFiles(string[] srcFilePaths, string destFilePath);

        Task AppendToFile(string path, string content);

        void DownloadFile(string srcFilePath, string destFilePath, bool overwrite = false);
    }
}