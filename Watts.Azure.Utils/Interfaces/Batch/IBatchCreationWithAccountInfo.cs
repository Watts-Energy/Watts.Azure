namespace Watts.Azure.Utils.Interfaces.Batch
{
    using Microsoft.Azure.Batch.Auth;
    using Watts.Azure.Common.Storage.Objects;

    public interface IBatchCreationWithAccountInfo
    {
        BatchSharedKeyCredentials Credentials { get; set; }

        IBatchCreationWithBatchAndStorageAccountSettings UsingStorageAccountSettings(
            StorageAccountSettings storageAccountSettings);
    }
}