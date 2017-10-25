namespace Watts.Azure.Common.ServiceBus.Objects
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Interfaces.General;
    using Interfaces.ServiceBus;
    using Interfaces.Wrappers;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class AzureServiceBusTopic : ITopicBus
    {
        /// <summary>
        /// The filter to apply when subscribing to messages from the topic
        /// </summary>
        private string sqlFilter;

        private readonly string connectionString;
        private readonly string topicName;
        private readonly string subscriptionId;

        private readonly INamespaceManager namespaceManager;
        private readonly ITopicClient topicClient;
        private readonly ITopicSubscriptionClient topicSubscriptionClient;

        /// <summary>
        /// Create a new instance of AzureServiceBusTopic
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="connectionString"></param>
        /// <param name="namespaceManager"></param>
        /// <param name="topicClient"></param>
        /// <param name="topicSubscriptionClient"></param>
        public AzureServiceBusTopic(string topicName, string subscriptionId, string connectionString, INamespaceManager namespaceManager, ITopicClient topicClient, ITopicSubscriptionClient topicSubscriptionClient)
        {
            this.topicName = topicName;
            this.subscriptionId = subscriptionId;
            this.connectionString = connectionString;
            this.namespaceManager = namespaceManager;
            this.topicClient = topicClient;
            this.topicSubscriptionClient = topicSubscriptionClient;
        }

        /// <summary>
        /// The service bus namespace name
        /// </summary>
        public string NamespaceName => new Regex("Endpoint=sb:\\/\\/(.*?).servicebus").Match(this.connectionString).Groups[1].Value;

        public void SetFilter(string filter)
        {
            this.sqlFilter = filter;
        }

        /// <summary>
        /// Initializing the topic ensures that 
        /// a) The topic exists
        /// b) if recreateSubscription is true, that it exists
        /// </summary>
        /// <param name="recreateSubscription"></param>
        /// <param name="receiveMode"></param>
        public void Initialize(bool recreateSubscription, ReceiveMode receiveMode = ReceiveMode.PeekLock)
        {
            this.CreateTopicIfNotExists();
            this.CreateSubscriptionIfDoesntExist(recreateSubscription);
        }

        public async Task SendMessage(object messageObject)
        {
            await this.topicClient.SendAsync(messageObject.ToBrokeredMessage());
        }

        public async Task SendMessage(BrokeredMessage message)
        {
            await this.topicClient.SendAsync(message);
        }

        public void Subscribe(Action<BrokeredMessage> subscriptionCallback)
        {
            this.topicSubscriptionClient.OnMessage(subscriptionCallback);
        }

        internal void CreateTopicIfNotExists()
        {
            if (!this.namespaceManager.TopicExists(this.topicName))
            {
                var td = new TopicDescription(this.topicName);
                this.namespaceManager.CreateTopic(td);
            }
        }

        internal void CreateSubscriptionIfDoesntExist(bool recreateSubscription)
        {
            bool exists = this.namespaceManager.SubscriptionExists(this.topicName, this.subscriptionId);
            bool deleted = false;

            if (exists && recreateSubscription)
            {
                this.namespaceManager.DeleteSubscription(this.topicName, this.subscriptionId);
                deleted = true;
            }

            if (deleted || (exists && !recreateSubscription))
            {
                if (!string.IsNullOrEmpty(this.sqlFilter))
                {
                    this.namespaceManager.CreateSubscription(this.topicName, this.subscriptionId, new SqlFilter(this.sqlFilter));
                }
                else
                {
                    this.namespaceManager.CreateSubscription(this.topicName, this.subscriptionId);
                }
            }
        }
    }
}