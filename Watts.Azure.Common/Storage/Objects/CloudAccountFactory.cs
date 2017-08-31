namespace Watts.Azure.Common.Storage.Objects
{
    using Interfaces.General;
    using Interfaces.Wrappers;
    using Wrappers;

    public class CloudAccountFactory : ICloudAccountFactory
    {
        public IStorageAccount GetStorageAccount(string connectionString)
        {
            return StorageAccount.Parse(connectionString);
        }
    }
}