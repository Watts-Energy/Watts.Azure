namespace Watts.Azure.Common.ServiceBus.Objects
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Interfaces.General;
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

        private TopicClient topicClient;
        private SubscriptionClient receiveClient;
        private readonly NamespaceManager namespaceManager;

        /// <summary>
        /// Create a new instance of AzureServiceBusTopic
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="connectionString"></param>
        public AzureServiceBusTopic(string topicName, string subscriptionId, string connectionString)
        {
            this.topicName = topicName;
            this.subscriptionId = subscriptionId;
            this.connectionString = connectionString;
            this.namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
        }

        /// <summary>
        /// 
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
        /// c) that the topicclient is initialized and finally
        /// d) that a subscriptionclient exists, to receive messages from the bus.
        /// </summary>
        /// <param name="recreateSubscription"></param>
        /// <param name="receiveMode"></param>
        public void Initialize(bool recreateSubscription, ReceiveMode receiveMode = ReceiveMode.PeekLock)
        {
            this.CreateTopicIfNotExists();
            this.CreateSubscriptionIfDoesntExist(recreateSubscription);

            this.topicClient = TopicClient.CreateFromConnectionString(this.connectionString, this.topicName);

            this.receiveClient = SubscriptionClient.CreateFromConnectionString(this.connectionString, this.topicName, this.subscriptionId, receiveMode);
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
            this.receiveClient.OnMessage(subscriptionCallback);
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