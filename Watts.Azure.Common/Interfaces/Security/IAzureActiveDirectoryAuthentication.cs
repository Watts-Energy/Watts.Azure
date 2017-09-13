namespace Watts.Azure.Common.Interfaces.Security
{
    using Microsoft.Azure;
    using Microsoft.Rest;

    /// <summary>
    /// Interface for an Azure Active Directory authentication
    /// </summary>
    public interface IAzureActiveDirectoryAuthentication
    {
        string GetAuthorizationToken();

        TokenCloudCredentials GetTokenCredentials();

        ServiceClientCredentials GetServiceCredentials();
    }
}