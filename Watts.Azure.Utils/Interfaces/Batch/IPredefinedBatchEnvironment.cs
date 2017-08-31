namespace Watts.Azure.Utils.Interfaces.Batch
{
    using Common.Batch.Objects;
    using Common.Storage.Objects;

    /// <summary>
    /// Interface for a predefined environment where batch account and storage account settings have been specified.
    /// </summary>
    public interface IPredefinedBatchEnvironment
    {
        BatchAccountSettings BatchAccountSettings { get; set; }

        StorageAccountSettings BatchStorageAccountSettings { get; set; }

        bool IsValid();

        string GetBatchStorageConnectionString();
    }
}