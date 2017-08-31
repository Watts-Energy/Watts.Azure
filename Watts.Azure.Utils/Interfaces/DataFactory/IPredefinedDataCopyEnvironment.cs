namespace Watts.Azure.Utils.Interfaces.DataFactory
{
    using Watts.Azure.Common.DataFactory.General;
    using Watts.Azure.Common.Storage.Objects;

    public interface IPredefinedDataCopyEnvironment
    {
        /// <summary>
        /// The id of your Azure subscription.
        /// </summary>
        string SubscriptionId { get; set; }

        /// <summary>
        /// The client id of the app you register with Azure AD (which you must in order to run Azure Data Factory). How to, see e.g. (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        string AdfClientId { get; set; }

        /// <summary>
        /// Client secret for the app you register with Azure AD (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        string ClientSecret { get; set; }

        /// <summary>
        /// The Azure Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
        /// </summary>
        string ActiveDirectoryTenantId { get; set; }

        StorageAccountSettings StorageAccountSettings { get; set; }

        AzureDataFactorySetup DataFactorySetup { get; set; }

        string GetDataFactoryStorageAccountConnectionString();
    }
}