namespace Watts.Azure.Utils.Helpers.Batch
{
    using Build;
    using Common.Batch.Objects;
    using Common.Storage.Objects;
    using Microsoft.Azure.Batch.Auth;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch creation facade with account info specified.
    /// </summary>
    public class BatchCreationWithBatchAccountInfo : BatchBuilder, IBatchCreationWithAccountInfo
    {
        public BatchCreationWithBatchAccountInfo(BatchCreationWithBatchAccountInfo parent)
        {
            this.BatchAccountSettings = parent?.BatchAccountSettings;

            this.Credentials = parent?.Credentials;
        }

        public BatchCreationWithBatchAccountInfo(BatchAccountSettings accountSettings)
        {
            this.BatchAccountSettings = accountSettings;
            this.Credentials = new BatchSharedKeyCredentials(
               this.BatchAccountSettings.BatchAccountUrl,
               this.BatchAccountSettings.BatchAccountName,
               this.BatchAccountSettings.BatchAccountKey);
        }

        public BatchAccountSettings BatchAccountSettings { get; set; }

        public BatchSharedKeyCredentials Credentials { get; set; }

        public IBatchCreationWithBatchAndStorageAccountSettings UsingStorageAccountSettings(
           StorageAccountSettings storageAccountSettings)
        {
            return new BatchCreationWithBatchAndStorageAccountSettings(this, storageAccountSettings);
        }
    }
}