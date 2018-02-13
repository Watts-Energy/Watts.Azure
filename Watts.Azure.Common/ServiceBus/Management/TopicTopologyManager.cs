namespace Watts.Azure.Common.ServiceBus.Management
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces.ServiceBus;
    using Objects;

    /// <summary>
    /// Manage the assignment of topic instances to callers, based on which endpoints are currently full and which have free space.
    /// </summary>
    public class TopicTopologyManager
    {
        private readonly AzureServiceBusTopology topology;
        private readonly List<TopicAssignmentInfo> assignments = new List<TopicAssignmentInfo>();

        /// <summary>
        /// Create a manager that handles the assignment of topic subscriptions to callers, based on the scale mode.
        /// </summary>
        /// <param name="topology">The topology, which may be duplicated if the scaleMode is Horizontal (see scaleMode)</param>
        public TopicTopologyManager(AzureServiceBusTopology topology)
        {
            this.topology = topology;

            // Initialize an object to keep track of the number of subscriptions currently assigned to eaceh topic.
            this.topology.GetLeafNodes().ToList().ForEach(p => this.assignments.Add(new TopicAssignmentInfo(p.Value, 0, this.topology.MaxSubscribersPerTopic)));
        }

        public AzureServiceBusTopicInfo GetRootTopicInfo()
        {
            return this.topology.GetRootTopic();
        }

        /// <summary>
        /// We're full if we've assigned all the endpoints we've created.
        /// </summary>
        public bool IsFull => this.assignments.Count == 0;

        public AzureServiceBusTopicSubscriptionInfo GetTopicSubcriptionInfo()
        {
            if (this.assignments.Count == 0)
            {
                throw new InvalidOperationException("All topics are full...");
            }

            var firstAvailable = this.assignments[0];

            AzureServiceBusTopicSubscriptionInfo retVal = new AzureServiceBusTopicSubscriptionInfo(firstAvailable.TopicInfo.Name, firstAvailable.TopicInfo.PrimaryConnectionString, $"{firstAvailable.TopicInfo.Name}-{firstAvailable.NumberOfSubscriptionsCreated}");

            firstAvailable.NumberOfSubscriptionsCreated++;

            // If we've reached capacity for the topic, remove it from the list...
            if (firstAvailable.IsFull)
            {
                this.assignments.RemoveAt(0);
            }

            return retVal;
        }

        /// <summary>
        /// Get a subscription topic, i.e. one at the leaf level of the topology.
        /// </summary>
        /// <returns></returns>
        internal AzureServiceBusTopicSubscriptionInfo GetTopicInstance()
        {
            if (this.assignments.Count == 0)
            {
                throw new InvalidOperationException("All topics are full...");
            }

            var firstAvailable = this.assignments[0];

            AzureServiceBusTopicSubscriptionInfo retVal = new AzureServiceBusTopicSubscriptionInfo(this.topology.TopicName, firstAvailable.TopicInfo.PrimaryConnectionString, $"{firstAvailable.TopicInfo.Name}-{firstAvailable.NumberOfSubscriptionsCreated}");

            firstAvailable.NumberOfSubscriptionsCreated++;

            // If we've reached capacity for the topic, remove it from the list...
            if (firstAvailable.IsFull)
            {
                this.assignments.RemoveAt(0);
            }

            return retVal;
        }

        /// <summary>
        /// Get a root topic. If the scaleMode is Horizontal, this will be a copy of the original topic.
        /// </summary>
        /// <returns></returns>
        internal AzureServiceBusTopic GetRootTopic()
        {
            var root = this.topology.GetRootTopic();

            AzureServiceBusTopic rootTopic = new AzureServiceBusTopic(this.topology.TopicName,
                $"{this.topology.TopicName}-sub", root.PrimaryConnectionString);

            return rootTopic;
        }
    }
}