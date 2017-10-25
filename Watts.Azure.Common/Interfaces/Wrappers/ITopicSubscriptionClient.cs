namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    public interface ITopicSubscriptionClient
    {
        void OnMessage(Action<BrokeredMessage> callback);
    }
}