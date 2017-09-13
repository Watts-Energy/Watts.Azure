namespace Watts.Azure.Utils.Objects
{
    public class PredefinedDataLakeStoreEnvironment
    {
        public PredefinedDataLakeStoreEnvironment()
        {
        }

        public PredefinedDataLakeStoreEnvironment(string subscriptionId, string adfClientId, string clientSecret, string tenantId, string dataLakeStoreName)
        {
            this.SubscriptionId = subscriptionId;
            this.AdfClientId = adfClientId;
            this.ClientSecret = clientSecret;
            this.ActiveDirectoryTenantId = tenantId;
            this.DataLakeStoreName = dataLakeStoreName;
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
        /// The name of the data lake store
        /// </summary>
        public string DataLakeStoreName { get; set; }
    }
}