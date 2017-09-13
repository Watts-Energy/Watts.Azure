namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.DataLake.Store;
    using Microsoft.Azure.Management.DataLake.Store.Models;
    using Watts.Azure.Common.Interfaces.DataFactory;
    using Watts.Azure.Common.Interfaces.Security;
    using Watts.Azure.Common.Interfaces.Storage;

    public class AzureDataLakeStore : IAzureDataLakeStore
    {
        private IAzureActiveDirectoryAuthentication authenticator;
        private DataLakeStoreAccountManagementClient client;
        private DataLakeStoreFileSystemManagementClient fileSystemClient;

        public AzureDataLakeStore(string subscriptionId, string directory, string name, IAzureActiveDirectoryAuthentication authenticator)
        {
            this.Name = name;
            this.Directory = directory;
            this.ConnectionString = $"https://{this.Name}.azuredatalakestore.net/webhdfs/v1";
            this.authenticator = authenticator;

            // Authenticate...
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var serviceCredentials = authenticator.GetServiceCredentials();
            this.client = new DataLakeStoreAccountManagementClient(serviceCredentials)
            {
                SubscriptionId = subscriptionId
            };
            this.fileSystemClient = new DataLakeStoreFileSystemManagementClient(serviceCredentials);
        }

        public IAzureActiveDirectoryAuthentication Authenticator
        {
            get
            {
                return this.authenticator;
            }
        }

        /// <summary>
        /// The name of the data lake store.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The base directory. All paths specified in methods are relative to this.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Get the connection string for the Data lake store
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Has no meaning in AzureDataLakeStore.cs but must be implemenented as IAzureLinkedService. 
        /// TODO find a way to handle this so that not all IAzureLinkedService's must implement it...
        /// </summary>
        /// <param name="partitionKeyType"></param>
        /// <param name="rowKeyType"></param>
        /// <returns></returns>
        public DataStructure GetStructure(string partitionKeyType = null, string rowKeyType = null)
        {
            return new DataStructure();
        }

        /// <summary>
        /// Creates a directory at the specified path, relative to the Directory property.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public async Task CreateDirectory(string relativePath)
        {
            string path = string.Join("", this.Directory, relativePath);

            await this.fileSystemClient.FileSystem.MkdirsAsync(this.Name, path);
        }

        /// <summary>
        /// Delete the directory at the given path (relative to this.Directory), if it exists. 
        /// If recursive = false and the folder is not empty, an exception is thrown.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public async Task DeleteDirectory(string relativePath = "", bool recursive = false)
        {
            string path = string.Join("", this.Directory, relativePath);

            if (this.PathExists(path))
            {
                // If it is not a directory, throw an exception.
                if(this.GetItemInfo(path).Type != FileType.DIRECTORY)
                {
                    throw new ArgumentException($"{path} is not a directory. Please use DeleteFile instead");
                }

                await this.fileSystemClient.FileSystem.DeleteAsync(this.Name, path, recursive);
            }
        }

        /// <summary>
        /// Delete the file at the specified (relative) path, if it exists.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public async Task DeleteFile(string relativePath)
        {
            string path = string.Join(string.Empty, this.Directory, relativePath);

            if (this.PathExists(path))
            {
                // If it is not a file, but a directory instead, throw an exception.
                if (this.GetItemInfo(path).Type != FileType.FILE)
                {
                    throw new ArgumentException($"{path} is a directory - please use DeleteDirectory instead...");
                }

                await this.fileSystemClient.FileSystem.DeleteAsync(this.Name, path, recursive: true);
            }
        }

        /// <summary>
        /// Get a boolean indicating whether the path exists in the data store.
        /// NOTE that this is an absolute path, i.e. not relative to the root Directory property.
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public bool PathExists(string absolutePath)
        {
            return this.fileSystemClient.FileSystem.PathExists(this.Name, absolutePath);
        }

        /// <summary>
        /// Upload a file to the data lake store. It is placed at the selected destination, relative to the Directory this points to.
        /// </summary>
        /// <param name="localSourceFilePath">The local path to the file to upload</param>
        /// <param name="destinationRelativeFilePath">The relative (to this.Directory) path to place the file.</param>
        /// <param name="force">If true, overwrites the file if it exists. If false, throws an exception.</param>
        public void UploadFile(string localSourceFilePath, string destinationRelativeFilePath, bool force = true)
        {
            this.fileSystemClient.FileSystem.UploadFile(this.Name,   localSourceFilePath, string.Join("/", this.Directory, destinationRelativeFilePath), overwrite: force);
        }

        /// <summary>
        /// Get information about the file/directory at the specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public FileStatusProperties GetItemInfo(string path)
        {
            return this.fileSystemClient.FileSystem.GetFileStatusAsync(this.Name, path).Result.FileStatus;
        }

        /// <summary>
        /// List the items at the specified path.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public List<FileStatusProperties> ListItems(string directoryPath)
        {
            return this.fileSystemClient.FileSystem.ListFileStatus(this.Name, directoryPath).FileStatuses.FileStatus.ToList();
        }

        /// <summary>
        /// Concatenate the files at 'srcFilePaths' into one file at 'destFilePath'.
        /// NOTE that this deletes the files at 'srcFilePaths'
        /// </summary>
        /// <param name="srcFilePaths"></param>
        /// <param name="destFilePath"></param>
        /// <returns></returns>
        public async Task ConcatenateFiles(string[] srcFilePaths, string destFilePath)
        {
            await this.fileSystemClient.FileSystem.ConcatAsync(this.Name, destFilePath, srcFilePaths);
        }

        /// <summary>
        /// Append the content to the file at the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task AppendToFile(string path, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                await this.fileSystemClient.FileSystem.AppendAsync(this.Name, path, stream);
            }
        }

        /// <summary>
        /// Download a file from the Data lake store to a local file. 
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="destFilePath"></param>
        /// <param name="overwrite"></param>
        public void DownloadFile(string srcFilePath, string destFilePath, bool overwrite = false)
        {
            this.fileSystemClient.FileSystem.DownloadFile(this.Name, srcFilePath, destFilePath, overwrite: overwrite);
        }
    }
}