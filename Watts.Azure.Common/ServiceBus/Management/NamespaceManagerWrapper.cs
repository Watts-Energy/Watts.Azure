namespace Watts.Azure.Common.ServiceBus.Management
{
    using Interfaces.Wrappers;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class NamespaceManagerWrapper : INamespaceManager
    {
        private readonly NamespaceManager namespaceManager;

        private NamespaceManagerWrapper(string connectionString)
        {
            this.namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
        }

        public static NamespaceManagerWrapper CreateFromConnectionString(string connectionString)
        {
            return new NamespaceManagerWrapper(connectionString);
        }

        public bool TopicExists(string path)
        {
            return this.namespaceManager.TopicExists(path);
        }

        public bool SubscriptionExists(string topicPath, string name)
        {
            return this.namespaceManager.SubscriptionExists(topicPath, name);
        }

        public void CreateTopic(TopicDescription description)
        {
            this.namespaceManager.CreateTopic(description);
        }

        public SubscriptionDescription CreateSubscription(string topicPath, string name, Filter filter)
        {
            return this.namespaceManager.CreateSubscription(topicPath, name, filter);
        }

        public SubscriptionDescription CreateSubscription(string topicPath, string name)
        {
            return this.namespaceManager.CreateSubscription(topicPath, name);
        }

        public void DeleteSubscription(string topicPath, string name)
        {
            this.namespaceManager.DeleteSubscription(topicPath, name);
        }
    }
}