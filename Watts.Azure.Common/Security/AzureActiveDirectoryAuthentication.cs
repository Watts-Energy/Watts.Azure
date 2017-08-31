namespace Watts.Azure.Common.Security
{
    using System;
    using Interfaces.Security;
    using Microsoft.Azure;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    public class AzureActiveDirectoryAuthentication : IAzureActiveDirectoryAuthentication
    {
        private readonly string subscriptionId;
        private readonly AppActiveDirectoryAuthenticationCredentials credentials;

        public AzureActiveDirectoryAuthentication(string subscriptionId, AppActiveDirectoryAuthenticationCredentials credentials)
        {
            this.subscriptionId = subscriptionId;
            this.credentials = credentials;
        }

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