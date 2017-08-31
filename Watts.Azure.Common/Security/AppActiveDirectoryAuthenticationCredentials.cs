namespace Watts.Azure.Common.Security
{
    /// <summary>
    /// Credentials for authenticating an application against Azure Active Directory.
    /// </summary>
    public class AppActiveDirectoryAuthenticationCredentials
    {
        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}