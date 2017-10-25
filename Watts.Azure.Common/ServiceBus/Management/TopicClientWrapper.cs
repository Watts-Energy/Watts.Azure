namespace Watts.Azure.Common.ServiceBus.Management
{
    using System.Threading.Tasks;
    using Interfaces.ServiceBus;
    using Interfaces.Wrappers;
    using Microsoft.ServiceBus.Messaging;

    public class TopicClientWrapper : ITopicClient
    {
        private readonly TopicClient topicClient;

        private TopicClientWrapper(string connectionString, string topicName)
        {
            this.topicClient = TopicClient.CreateFromConnectionString(connectionString, topicName);
        }

        public static TopicClientWrapper CreateFromConnectionString(string connectionString, string topicName)
        {
            return new TopicClientWrapper(connectionString, topicName);
        }

        public Task SendAsync(BrokeredMessage message)
        {
            return this.topicClient.SendAsync(message);
        }
    }
}