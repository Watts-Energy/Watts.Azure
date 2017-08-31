namespace Watts.Azure.Common.Interfaces.General
{
    using Wrappers;

    public interface ICloudAccountFactory
    {
        IStorageAccount GetStorageAccount(string connectionString);
    }
}