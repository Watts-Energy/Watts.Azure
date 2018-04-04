namespace Watts.Azure.Utils.Objects
{
    using Common.Security;
    using Watts.Azure.Common.DataFactory.General;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Utils.Interfaces.DataFactory;

    public class DataCopyEnvironment : IDataCopyEnvironment
    {
        public DataCopyEnvironment()
        {
        }

        public DataCopyEnvironment(string subscriptionId, AppActiveDirectoryAuthenticationCredentials credentials, StorageAccountSettings storageAccountSettings, AzureDataFactorySetup dataFactorySetup)
        {
            this.SubscriptionId = subscriptionId;
            this.Credentials = credentials;
            this.StorageAccountSettings = storageAccountSettings;
            this.DataFactorySetup = dataFactorySetup;
        }

        /// <summary>
        /// The id of your Azure subscription.
        /// </summary>
        public string SubscriptionId { get; set; }

        public AppActiveDirectoryAuthenticationCredentials Credentials { get; set; }

        /// <summary>
        /// Name and key of the storage account to perform the tests in.
        /// </summary>
        public StorageAccountSettings StorageAccountSettings { get; set; }

        public AzureDataFactorySetup DataFactorySetup { get; set; }

        public string GetDataFactoryStorageAccountConnectionString()
        {
            return $"DefaultEndpointsProtocol=https;AccountName={this.StorageAccountSettings.StorageAccountName};AccountKey={this.StorageAccountSettings.StorageAccountKey}";
        }
    }
}