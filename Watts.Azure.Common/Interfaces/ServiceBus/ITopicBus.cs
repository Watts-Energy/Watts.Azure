namespace Watts.Azure.Common.Interfaces.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public interface ITopicBus
    {
        bool IsInitialized { get; }

        string TopicName { get; }

        string TopicSubscriptionName { get; }

        void Initialize(bool recreateSubscription);

        Task SendMessageAsync(object messageObject);

        void Subscribe(Action<BrokeredMessage> subscriptionCallback);

        void SetFilter(string sqlFilter);

        void CreateIfNotExists(bool recreateSubscriptionEvenIfExists);
    }
}