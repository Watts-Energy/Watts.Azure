namespace Watts.Azure.Utils.Objects
{
    using Common;
    using Common.Security;

    public class AzureServiceBusEnvironment
    {
        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public string NamespaceName { get; set; }

        public AzureLocation Location { get; set; }

        public AppActiveDirectoryAuthenticationCredentials Credentials { get; set; }

        public string ServiceBusConnectionString { get; set; }
    }
}