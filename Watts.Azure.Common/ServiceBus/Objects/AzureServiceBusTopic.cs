namespace Watts.Azure.Common.ServiceBus.Objects
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Exceptions;
    using General;
    using Interfaces.ServiceBus;
    using Interfaces.Wrappers;
    using Management;
    using Microsoft.ServiceBus.Messaging;

    public class AzureServiceBusTopic : ITopicBus
    {
        /// <summary>
        /// The filter to apply when subscribing to messages from the topic
        /// </summary>
        private string sqlFilter;

        private readonly string connectionString;
        private readonly string topicName;
        private readonly string topicSubscriptionName;

        private readonly INamespaceManager namespaceManager;
        private readonly ITopicClient topicClient;
        private ITopicSubscriptionClient topicSubscriptionClient;

        private bool isInitialized = false;

        public AzureServiceBusTopic(string topicName, string topicSubscriptionName, string connectionString)
        {
            this.topicName = topicName;
            this.topicSubscriptionName = topicSubscriptionName;
            this.connectionString = connectionString;

            this.namespaceManager = NamespaceManagerWrapper.CreateFromConnectionString(this.connectionString);
            this.topicClient = TopicClientWrapper.CreateFromConnectionString(this.connectionString, this.topicName);

            // If there is a topic subscription name, create the subscription client.
            if (!string.IsNullOrEmpty(this.topicSubscriptionName))
            {
                this.topicSubscriptionClient = new TopicSubscriptionClientWrapper(this.connectionString, this.topicName,
                    this.topicSubscriptionName);
            }
        }

        public bool IsInitialized => this.isInitialized;

        /// <summary>
        /// Create a new instance of AzureServiceBusTopic
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="topicSubscriptionName"></param>
        /// <param name="connectionString"></param>
        /// <param name="namespaceManager"></param>
        /// <param name="topicClient"></param>
        /// <param name="topicSubscriptionClient"></param>
        public AzureServiceBusTopic(string topicName, string topicSubscriptionName, string connectionString, INamespaceManager namespaceManager, ITopicClient topicClient, ITopicSubscriptionClient topicSubscriptionClient)
        {
            this.topicName = topicName;
            this.topicSubscriptionName = topicSubscriptionName;
            this.connectionString = connectionString;

            this.namespaceManager = namespaceManager;
            this.topicClient = topicClient;
            this.topicSubscriptionClient = topicSubscriptionClient;
        }

        public void CreateIfNotExists(bool recreateSubscriptionEvenIfExists)
        {
            this.CreateTopicIfNotExists();
            this.CreateSubscriptionIfDoesntExist(recreateSubscriptionEvenIfExists);
        }

        public int ClearMessages()
        {
            int numberOfMessagesCleared = 0;
            DateTime lastReceivedMessage = DateTime.Now;

            this.topicSubscriptionClient.OnMessage(p =>
            {
                p.Complete();
                lastReceivedMessage = DateTime.Now;
                numberOfMessagesCleared++;
            });

            // Continue popping and completing messages until we've not received any for a while.
            Retry.Do(() => (DateTime.Now - lastReceivedMessage).TotalSeconds > 5)
                .MaxTimes(10000)
                .WithDelayInMs(200)
                .Go();

            // Recreate the subscription client
            this.topicSubscriptionClient = new TopicSubscriptionClientWrapper(this.connectionString, this.topicName, this.topicSubscriptionName);
            this.CreateSubscriptionIfDoesntExist(true);

            return numberOfMessagesCleared;
        }

        /// <summary>
        /// The service bus namespace name
        /// </summary>
        public string NamespaceName => new Regex("Endpoint=sb:\\/\\/(.*?).servicebus").Match(this.connectionString).Groups[1].Value;

        public void SetFilter(string filter)
        {
            if (this.isInitialized)
            {
                throw new CannotSetFilterOnInitializedServiceBusException();    
            }

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
            if (this.isInitialized)
            {
                return;
            }

            this.CreateTopicIfNotExists();


            this.CreateSubscriptionIfDoesntExist(recreateSubscription);

            this.isInitialized = true;
        }

        public async Task SendMessageAsync(object messageObject)
        {
            await this.topicClient.SendAsync(messageObject.ToBrokeredMessage());
        }

        public async Task SendMessageAsync(BrokeredMessage message)
        {
            await this.topicClient.SendAsync(message);
        }

        public void SendMessage(BrokeredMessage message)
        {
            this.topicClient.Send(message);
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
            // If the topic subscription name is empty, just return
            if (string.IsNullOrEmpty(this.topicSubscriptionName))
            {
                return;
            }

            bool exists = this.namespaceManager.SubscriptionExists(this.topicName, this.topicSubscriptionName);
            bool deleted = false;

            if (exists && recreateSubscription)
            {
                this.namespaceManager.DeleteSubscription(this.topicName, this.topicSubscriptionName);
                deleted = true;
            }

            // If we just deleted the subscription, or if it already existed and we're supposed to recreaste it
            if (deleted || !exists)
            {
                if (!string.IsNullOrEmpty(this.sqlFilter))
                {
                    this.namespaceManager.CreateSubscription(this.topicName, this.topicSubscriptionName, new SqlFilter(this.sqlFilter));
                }
                else
                {
                    this.namespaceManager.CreateSubscription(this.topicName, this.topicSubscriptionName);
                }
            }
        }
    }
}