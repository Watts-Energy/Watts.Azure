namespace Watts.Azure.Utils.Objects
{
    using Common.Batch.Objects;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch.Auth;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// Base class for predefined batch environments.
    /// </summary>
    public class BatchEnvironment : IBatchEnvironment
    {
        public BatchAccountSettings BatchAccountSettings { get; set; }

        public StorageAccountSettings BatchStorageAccountSettings { get; set; }

        public BatchSharedKeyCredentials GetBatchAccountCredentials()
        {
            return new BatchSharedKeyCredentials(
                this.BatchAccountSettings.BatchAccountUrl,
                this.BatchAccountSettings.BatchAccountName,
                this.BatchAccountSettings.BatchAccountKey);
        }

        /// <summary>
        /// Checks whether all required information has been filled and returns true if is has, false otherwise.
        /// </summary>
        /// <returns>True if this environment is valid, false otherwise. Invalid means that not all required settings have been set.</returns>
        public bool IsValid()
        {
            return this.BatchAccountSettings != null &&
                   this.BatchStorageAccountSettings != null &&
                   !string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountKey) &&
                   !string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountName) &&
                   !string.IsNullOrEmpty(this.BatchAccountSettings.BatchAccountUrl) &&
                   !string.IsNullOrEmpty(this.BatchStorageAccountSettings.StorageAccountKey) &&
                   !string.IsNullOrEmpty(this.BatchStorageAccountSettings.StorageAccountName);
        }

        public string GetBatchStorageConnectionString()
        {
            return $"DefaultEndpointsProtocol=https;AccountName={this.BatchStorageAccountSettings.StorageAccountName};AccountKey={this.BatchStorageAccountSettings.StorageAccountKey}";
        }
    }
}