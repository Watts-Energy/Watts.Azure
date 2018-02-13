namespace Watts.Azure.Utils.Objects
{
    using Common.Security;

    public class DataLakeStoreEnvironment
    {
        public DataLakeStoreEnvironment()
        {
        }

        public DataLakeStoreEnvironment(string subscriptionId, string resourceGroupName, AppActiveDirectoryAuthenticationCredentials credentials, string dataLakeStoreName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
            this.Credentials = credentials;
            this.DataLakeStoreName = dataLakeStoreName;
        }

        /// <summary>
        /// The id of your Azure subscription.
        /// </summary>
        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public AppActiveDirectoryAuthenticationCredentials Credentials { get; set; }

        /// <summary>
        /// The name of the data lake store
        /// </summary>
        public string DataLakeStoreName { get; set; }
    }
}