namespace Watts.Azure.Common.Interfaces.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public interface ITopicBus
    {
        void Initialize(bool recreateSubscription, ReceiveMode receiveMode = ReceiveMode.PeekLock);

        Task SendMessageAsync(object messageObject);

        void Subscribe(Action<BrokeredMessage> subscriptionCallback);

        void SetFilter(string sqlFilter);
    }
}