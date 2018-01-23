namespace Watts.Azure.Common.ServiceBus.Objects
{
    public class AzureServiceBusTopicSubscriptionInfo : AzureServiceBusTopicInfo
    {
        public AzureServiceBusTopicSubscriptionInfo()
        {
        }

        public AzureServiceBusTopicSubscriptionInfo(string name, string primaryConnectionString,
            string subscriptionname)
            : base(name, primaryConnectionString)
        {
            this.SubscriptionName = subscriptionname;
        }

        public string SubscriptionName { get; set; }
    }
}