namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public interface ITopicClient
    {
        Task SendAsync(BrokeredMessage message);

        void Send(BrokeredMessage message);
    }
}