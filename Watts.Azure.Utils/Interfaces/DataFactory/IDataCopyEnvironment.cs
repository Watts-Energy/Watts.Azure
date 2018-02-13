namespace Watts.Azure.Utils.Interfaces.DataFactory
{
    using Common.Security;
    using Watts.Azure.Common.DataFactory.General;
    using Watts.Azure.Common.Storage.Objects;

    public interface IDataCopyEnvironment
    {
        string SubscriptionId { get; set; }

        AppActiveDirectoryAuthenticationCredentials Credentials { get; set; }

        StorageAccountSettings StorageAccountSettings { get; set; }

        AzureDataFactorySetup DataFactorySetup { get; set; }

        string GetDataFactoryStorageAccountConnectionString();
    }
}