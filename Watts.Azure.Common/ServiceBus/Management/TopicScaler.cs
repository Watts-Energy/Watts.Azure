namespace Watts.Azure.Common.ServiceBus.Management
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Interfaces.ServiceBus;
    using Objects;


    public class TopicScaler
    {
        private readonly string rootNamespace;
        private readonly string topicName;
        private readonly IAzureServiceBusManagement serviceBusManagement;
        private readonly AzureLocation location;
        private readonly int maxSubscribersPerInstance;
        private readonly ScaleMode scaleMode;
        private readonly int numberOfSubscribersPerInstance;

        private readonly TopicTopologyManager verticalScalingManager;
        private List<TopicTopologyManager> horizontalScalingManagers;

        private Random rand = new Random(DateTime.Now.Millisecond);

        private bool autoEmitTopologies;

        public TopicScaler(string rootNamespace, string topicName, IAzureServiceBusManagement serviceBusManagement,
            AzureLocation location, int numberOfSubscribersPerInstance, int maxSubscribersPerTopic = 2000, ScaleMode scaleMode = ScaleMode.Vertically, bool autoEmitTopologies = true)
        {
            this.rootNamespace = rootNamespace;
            this.topicName = topicName;
            this.serviceBusManagement = serviceBusManagement;
            this.location = location;
            this.numberOfSubscribersPerInstance = numberOfSubscribersPerInstance;
            this.maxSubscribersPerInstance = maxSubscribersPerTopic;
            this.scaleMode = scaleMode;
            this.autoEmitTopologies = autoEmitTopologies;

            this.verticalScalingManager = scaleMode == ScaleMode.Vertically ? new TopicTopologyManager(this.GetNextTopology()) : null;
            this.horizontalScalingManagers = scaleMode == ScaleMode.Horizontally ? new List<TopicTopologyManager>() : null;
        }

        /// <summary>
        /// Get a topic info suitable for sending messages on the topic (one that is at the root of the topology)
        /// </summary>
        /// <returns></returns>
        public AzureServiceBusTopicInfo GetTopicToSendOn()
        {
            if (this.scaleMode == ScaleMode.Vertically)
            {
                return this.verticalScalingManager.GetRootTopicInfo();
            }
            else
            {
                if (this.horizontalScalingManagers.Count == 0)
                {
                    this.horizontalScalingManagers = new List<TopicTopologyManager>()
                    {
                        new TopicTopologyManager(this.GetNextTopology())
                    };
                }

                // Select a random of the horizontally scaled topic topologies, so as to distribute the load on them.
                int randomIndex = this.rand.Next(this.horizontalScalingManagers.Count);
                return this.horizontalScalingManagers[randomIndex].GetRootTopicInfo();
            }
        }

        public List<AzureServiceBusTopicInfo> GetAllBroadcastTopics()
        {
            if (this.scaleMode == ScaleMode.Vertically)
            {
                return new List<AzureServiceBusTopicInfo>() {this.verticalScalingManager.GetRootTopicInfo()};
            }
            else
            {
                return this.horizontalScalingManagers.Select(p => p.GetRootTopicInfo()).ToList();
            }
        }

        /// <summary>
        /// Get a topic suitable for subscribing to messages on the topic. 
        /// </summary>
        /// <returns></returns>
        public AzureServiceBusTopicSubscriptionInfo GetTopicToSubscribeOn()
        {
            if (this.scaleMode == ScaleMode.Vertically)
            {
                // If we're meant to scale vertically we can let the vertical scaling manager handle this. It will give us a leaf node of the
                // topology.
                return this.verticalScalingManager.GetTopicSubcriptionInfo();
            }
            else
            {
                // We're meant to scale horizontally.
                // That means a new topic is created when the previous topic is full, and only a one-depth topology is supported.
                // First, check if we either don't have any managers, or the last one is full (we only add new ones once the previous is full,
                // so all previous topic subscriptions will be full)
                // If so, create one...
                if (this.horizontalScalingManagers.Count == 0 || this.horizontalScalingManagers.Last().IsFull)
                {
                    this.horizontalScalingManagers = new List<TopicTopologyManager>()
                    {
                        new TopicTopologyManager(this.GetNextTopology())
                    };
                }

                return this.horizontalScalingManagers.Last().GetTopicSubcriptionInfo();
            }
        }

        internal AzureServiceBusTopology GetNextTopology()
        {
            string nextTopicName = this.scaleMode == ScaleMode.Vertically ? this.topicName : $"{this.topicName}-{this.horizontalScalingManagers.Count + 1}";

            int numberOfSubscribers = this.scaleMode == ScaleMode.Vertically
                ? this.numberOfSubscribersPerInstance
                : this.maxSubscribersPerInstance;

            AzureServiceBusTopology retVal = new AzureServiceBusTopology(this.rootNamespace, nextTopicName, this.serviceBusManagement, this.location, this.maxSubscribersPerInstance);
            retVal.GenerateBusTopology(numberOfSubscribers);

            if (this.autoEmitTopologies)
            {
                retVal.Emit();
            }


            return retVal;
        }
    }
}
