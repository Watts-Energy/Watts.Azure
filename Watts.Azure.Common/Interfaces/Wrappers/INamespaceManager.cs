namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using Microsoft.ServiceBus.Messaging;

    public interface INamespaceManager
    {
        bool TopicExists(string path);

        bool SubscriptionExists(string topicPath, string name);

        void CreateTopic(TopicDescription description);

        SubscriptionDescription CreateSubscription(string topicPath, string name, Filter filter);

        SubscriptionDescription CreateSubscription(string topicPath, string name);

        void DeleteSubscription(string topicPath, string name);
    }
}