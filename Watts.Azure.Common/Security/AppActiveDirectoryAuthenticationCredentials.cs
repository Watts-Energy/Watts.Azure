namespace Watts.Azure.Common.Security
{
    /// <summary>
    /// Credentials for authenticating an application against Azure Active Directory.
    /// </summary>
    public class AppActiveDirectoryAuthenticationCredentials
    {
        /// <summary>
        /// The Azure Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The client id of the app you register with Azure AD (which you must in order to run Azure Data Factory). How to, see e.g. (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret for the app you register with Azure AD (see e.g. https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/)
        /// </summary>
        public string ClientSecret { get; set; }
    }
}