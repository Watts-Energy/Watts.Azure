namespace Watts.Azure.Utils.Objects
{
    using Watts.Azure.Common.DataFactory.General;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Utils.Interfaces.DataFactory;

    public class PredefinedDataCopyEnvironment : IPredefinedDataCopyEnvironment
    {
        public PredefinedDataCopyEnvironment()
        {
        }

        public PredefinedDataCopyEnvironment(string subscriptionId, string adfClientId, string clientSecret, string tenantId, StorageAccountSettings storageAccountSettings, AzureDataFactorySetup dataFactorySetup)
        {
            this.SubscriptionId = subscriptionId;
            this.AdfClientId = adfClientId;
            this.ClientSecret = clientSecret;
            this.ActiveDirectoryTenantId = tenantId;
            this.StorageAccountSettings = storageAccountSettings;
            this.DataFactorySetup = dataFactorySetup;
        }

        /// <summary>
        /// The id of your Azure subscription.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// The client id of the app you register with Azure AD (which you must in order to run Azure Data Factory). How to, see e.g. (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        public string AdfClientId { get; set; }

        /// <summary>
        /// Client secret for the app you register with Azure AD (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The Azure Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
        /// </summary>
        public string ActiveDirectoryTenantId { get; set; }

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