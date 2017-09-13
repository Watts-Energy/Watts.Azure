namespace Watts.Azure.Common.Interfaces.Security
{
    using Microsoft.Azure;
    using Microsoft.Rest;
    using Watts.Azure.Common.Security;

    /// <summary>
    /// Interface for an Azure Active Directory authentication
    /// </summary>
    public interface IAzureActiveDirectoryAuthentication
    {
        string SubscriptionId { get; }

        string ResourceGroupName { get; }

        AppActiveDirectoryAuthenticationCredentials Credentials { get; }

        string GetAuthorizationToken();

        TokenCloudCredentials GetTokenCredentials();

        ServiceClientCredentials GetServiceCredentials();
    }
}