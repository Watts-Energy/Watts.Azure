namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Management.Automation;
    using Interfaces.Storage;
    using Microsoft.WindowsAzure.Storage;
    using Watts.Azure.Common.Exceptions;

    /// <summary>
    /// An azure file share where files can be up- and downloaded.
    /// </summary>
    public class AzureFileshare : IFileshare
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly string shareName;

        public AzureFileshare(CloudStorageAccount storageAccount, string shareName)
        {
            this.storageAccount = storageAccount;
            this.shareName = shareName;
        }

        /// <summary>
        /// Connect to a fileshare using a connectionstring to the storage account and the name of the share.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="shareName"></param>
        /// <returns></returns>
        public static AzureFileshare Connect(string connectionString, string shareName)
        {
            return new AzureFileshare(CloudStorageAccount.Parse(connectionString), shareName);
        }

        /// <summary>
        /// Upload contained in a local file to the share
        /// </summary>
        /// <param name="localFilePath"></param>
        public void SaveDataToFile(string localFilePath)
        {
            // Create a CloudFileClient object for credentialed access to File storage.
            var fileClient = this.storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            var share = fileClient.GetShareReference(this.shareName);

            // Ensure that the share exists.
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                var shareRootDirectory = share.GetRootDirectoryReference();

                // Ensure that the directory exists.
                if (shareRootDirectory.Exists())
                {
                    // Get a reference to the file we created previously.
                    var fi = new FileInfo(localFilePath);
                    var cloudFile = shareRootDirectory.GetFileReference(fi.Name);
                    cloudFile.UploadFromFile(fi.FullName);
                }
            }
            else
            {
                throw new AzureFileshareDoesNotExistException(this.shareName);
            }
        }

        /// <summary>
        /// Download a file from the share to a local file at a given path.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="localFilePath"></param>
        public void DownloadFile(string filename, string localFilePath)
        {
            // Create a CloudFileClient object for credentialed access to File storage.
            var fileClient = this.storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            var share = fileClient.GetShareReference(this.shareName);

            // Ensure that the share exists.
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                var shareRootDirectory = share.GetRootDirectoryReference();

                // Ensure that the directory exists.
                if (shareRootDirectory.Exists())
                {
                    // Get a reference to the file we created previously.
                    var cloudFile = shareRootDirectory.GetFileReference(filename);
                    cloudFile.DownloadToFile(localFilePath, FileMode.CreateNew);
                }
            }
            else
            {
                throw new AzureFileshareDoesNotExistException(this.shareName);
            }
        }

        /// <summary>
        /// Create the file share if it doesn't already exist.
        /// </summary>
        /// <param name="storageAccountName"></param>
        /// <param name="storageAccountKey"></param>
        public void CreateIfDoesntExist(string storageAccountName, string storageAccountKey)
        {
            // Create a CloudFileClient object for credentialed access to File storage.
            var fileClient = this.storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            var share = fileClient.GetShareReference(this.shareName);

            // Ensure that the share exists.
            if (!share.Exists())
            {
                using (PowerShell powershellInstance = PowerShell.Create())
                {
                    powershellInstance.AddScript("Import-Module Azure.Storage");
                    powershellInstance.AddScript($"$storageContext = New-AzureStorageContext {storageAccountName} {storageAccountKey}");
                    powershellInstance.AddScript($"$share = New-AzureStorageShare {this.shareName} -Context $storageContext");

                    powershellInstance.Invoke();

                    if(powershellInstance.Streams.Error.Count > 0)
                    {
                        List<Exception> errors = new List<Exception>();

                        foreach(var error in powershellInstance.Streams.Error)
                        {
                            errors.Add(error.Exception);
                        }

                        throw new AggregateException(errors);
                    }
                }
            }
        }
    }
}