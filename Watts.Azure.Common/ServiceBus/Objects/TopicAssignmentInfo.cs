namespace Watts.Azure.Common.ServiceBus.Objects
{
    public class TopicAssignmentInfo
    {
        public TopicAssignmentInfo(AzureServiceBusTopicInfo topicInfo, int numberOfSubscriptionsCreated,
            int subscriptionCapacity)
        {
            this.TopicInfo = topicInfo;
            this.NumberOfSubscriptionsCreated = numberOfSubscriptionsCreated;
            this.SubscriptionCapacity = subscriptionCapacity;
        }

        public AzureServiceBusTopicInfo TopicInfo { get; set; }

        public int NumberOfSubscriptionsCreated { get; set; }

        public int SubscriptionCapacity { get; set; }

        public bool IsFull => this.NumberOfSubscriptionsCreated >= this.SubscriptionCapacity;
    }
}