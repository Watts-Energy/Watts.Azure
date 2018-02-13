namespace Watts.Azure.Common.Security
{
    using System;
    using Interfaces.Security;
    using Microsoft.Azure;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure.Authentication;

    public class AzureActiveDirectoryAuthentication : IAzureActiveDirectoryAuthentication
    {
        private readonly string subscriptionId;
        private readonly AppActiveDirectoryAuthenticationCredentials credentials;

        public AzureActiveDirectoryAuthentication(string subscriptionId, string resourceGroupName, AppActiveDirectoryAuthenticationCredentials credentials)
        {
            this.subscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
            this.credentials = credentials;
        }

        public string SubscriptionId => this.subscriptionId;

        public string ResourceGroupName { get; }

        public AppActiveDirectoryAuthenticationCredentials Credentials => this.credentials;

        public string GetAuthorizationToken()
        {
            string tenantId = this.credentials.TenantId;

            string managementEndpoint = Constants.WindowsManagementUri;

            var authenticationContext = new AuthenticationContext($"{Constants.ActiveDirectoryEndpoint}/{tenantId}");
            ClientCredential credential = new ClientCredential(this.credentials.ClientId, this.credentials.ClientSecret);
            var result = authenticationContext.AcquireTokenAsync(resource: managementEndpoint, clientCredential: credential).Result;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }

        public ServiceClientCredentials GetServiceCredentials()
        {
            ClientCredential credential = new ClientCredential(this.credentials.ClientId, this.credentials.ClientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(this.credentials.TenantId, credential);

            return creds.Result;
        }

        public TokenCloudCredentials GetTokenCredentials()
        {
            TokenCloudCredentials aadTokenCredentials =
                new TokenCloudCredentials(
                    this.subscriptionId,
                    this.GetAuthorizationToken());

            return aadTokenCredentials;
        }
    }
}