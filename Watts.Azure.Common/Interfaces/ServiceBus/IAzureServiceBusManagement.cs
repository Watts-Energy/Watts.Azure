namespace Watts.Azure.Common.Interfaces.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.ServiceBus.Objects;
    using Microsoft.Azure.Management.ServiceBus.Models;

    public interface IAzureServiceBusManagement
    {
        Task<List<SBNamespace>> GetNamespacesAsync();

        SBNamespace GetNamespace(string name);

        SBNamespace CreateOrUpdateNamespace(string namespaceName, AzureLocation location);

        AzureServiceBusTopicInfo CreateOrUpdateTopic(string namespaceName, string topicName);

        SBTopic GetTopic(string namespaceName, string topicName);

        List<SBTopic> GetTopics(string namespaceName, string startsWith = null);

        void DeleteTopic(string namespaceName, string topicName);

        string GetNamespaceConnectionString(string namespaceName);

        void DeleteTopics(string namespaceName, string topicNamePattern, Action<string> reportProgressDelegate = null);
    }
}