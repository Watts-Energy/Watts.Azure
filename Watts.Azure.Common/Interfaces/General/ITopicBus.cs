namespace Watts.Azure.Common.Interfaces.General
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public interface ITopicBus
    {
        void Initialize(bool recreateSubscription, ReceiveMode receiveMode = ReceiveMode.PeekLock);

        Task SendMessage(object messageObject);

        void Subscribe(Action<BrokeredMessage> subscriptionCallback);

        void SetFilter(string sqlFilter);
    }
}