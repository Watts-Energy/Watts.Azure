namespace Watts.Azure.Common.ServiceBus.Management
{
    using System;
    using System.Linq;
    using General;
    using Interfaces.ServiceBus;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Objects;

    /// <summary>
    /// This class represents a certain topology in Azure Service Bus. It can create auto-forwarding topics etc in a tree-structure that allows
    /// for scaling beyond the 2000 subscriptions per topic currently imposed in Azure Service Bus.
    /// It is possible to specify the maximum number of subscribers that should be allowed. At the time of writing, the hard limit in Azure is 2000 subscriptions per topic,
    /// but this could change in the future so no hard limit is enforced here.
    /// </summary>
    public class AzureServiceBusTopology
    {
        /// <summary>
        /// The number of times that we should retry when Azure service bus management throws an error
        /// </summary>
        private const int NumberOfRetriesOnFailure = 5;

        /// <summary>
        /// The number of milliseconds delay between each retry attempt
        /// </summary>
        private const int MillisecondDelayBetweenAttemptsOnFailure = 500;

        /// <summary>
        /// The maximum number of subscribers per service topic
        /// </summary>
        private readonly int maxSubscribersPerTopic;

        /// <summary>
        /// The name of the root service bus namespace. Sub-namespaces names are derived from this, when necessary
        /// </summary>
        private readonly string rootNamespaceName;

        /// <summary>
        /// The name of the topic
        /// </summary>
        private readonly string topicName;

        /// <summary>
        /// The root of the topology. This represents the root topic.
        /// If the number of required subscribers to the topic exceeds the maximum threshold, sub-topics are created and the root forwards to these.
        /// This process is repeated until there are enough levels to support the required number of subscribers
        /// </summary>
        private TreeNode<AzureServiceBusTopicInfo> rootNode;

        /// <summary>
        /// The location where the service bus should be located
        /// </summary>
        private readonly AzureLocation location;

        /// <summary>
        /// Management client for CRUD operations on the service bus API
        /// </summary>
        private readonly IAzureServiceBusManagement management;

        /// <summary>
        /// Keeps track of the total number of topics that have been processed (created or deleted)
        /// </summary>
        private int numberOfTopicsProcessed = 0;

        /// <summary>
        /// Keeps track of the total number of subscriptions that have been processed (created or deleted)
        /// </summary>
        private int numberOfSubscriptionsProcessed = 0;

        /// <summary>
        /// Callback action to report progress/events on.
        /// </summary>
        private Action<string> reportAction;

        /// <summary>
        /// Creates a new instance of AzureServiceBusTopology
        /// </summary>
        /// <param name="rootNamespace">The root service bus namespace name</param>
        /// <param name="topicName">The name of the topic to scale</param>
        /// <param name="serviceBusManagement">Management client for Azure Service Bus</param>
        /// <param name="location">Location to create the bus in</param>
        /// <param name="maxSubscribersPerTopic">(optional) The maximum number of subscribers to allow per topic.</param>
        public AzureServiceBusTopology(string rootNamespace, string topicName, IAzureServiceBusManagement serviceBusManagement, AzureLocation location, int maxSubscribersPerTopic = 2000)
        {
            this.rootNamespaceName = rootNamespace;
            this.maxSubscribersPerTopic = maxSubscribersPerTopic;
            this.topicName = topicName;
            this.location = location;

            // Create the management client for CRUD operations on namespaces and topics
            this.management = serviceBusManagement;
        }

        /// <summary>
        /// Get or set the total number of nodes.
        /// </summary>
        public int TotalNumberOfNodes { get; set; } = 1;

        /// <summary>
        /// Specify an action to report information/progress on.
        /// </summary>
        /// <param name="action"></param>
        public void ReportOn(Action<string> action)
        {
            this.reportAction = action;
        }

        /// <summary>
        /// Generate a topology that would support the given number of subscribers.
        /// E.g. if the maximum number of subscribers per topic is 2000 and we need to support 10000 subscribers, we will need to create one 'root' topic
        /// and five children, which the root topic will auto-forward to.
        /// </summary>
        public void GenerateBusTopology(int numberOfSubscribers)
        {
            this.Report("Creating topology...");

            // Determine the number of 'levels' that we need to create.
            // E.g. let's say there are 10,000 subscribers. Each namespace topic can handle a total of 2000 subscriptions by default, i.e. we'll need to add another level.
            // One root, with 5 sub-namespaces that it forwards to and
            // five sub-namespaces with 2000 subscriptions for the topic in each.
            // If the number of subscribers exceeds 2000*2000 = 4,000,000 (i.e. one root with 2000 subscribers with 2000 subscribers each), we will have to create e third level.
            // Formally, the number of levels we'll need is log_maxSubsribersPerNamespace(number of subscribers)
            int numberOfLevels = (int)Math.Ceiling(Math.Log(numberOfSubscribers, this.maxSubscribersPerTopic));
            this.Report($"There will be {numberOfLevels} levels");

            this.rootNode = new TreeNode<AzureServiceBusTopicInfo>(new AzureServiceBusTopicInfo()
            {
                Name = this.topicName
            });

            // Fill all levels up to the leaf level
            this.FillWithChildTopics(this.rootNode, this.maxSubscribersPerTopic, 1, (int)Math.Max(1, numberOfLevels - 1));

            // There can be maxNumberOfSubscribers per leaf node, meaning that the total number of available leaf nodes should be
            // numberOfSubscribers / maxSubscribersPerNamespace
            int requiredLeaves = numberOfSubscribers / this.maxSubscribersPerTopic;

            var leaves = this.rootNode.FlattenNodes().Where(p => p.IsLeaf).ToList();

            // The number of 'leaves' in our tree of Topics will need to be numberOfSubscribers / maxNumberOfSubscribers.
            // E.g. 10000 subscribers with max subscribers equal to 2000 will mean that there needs to be 5 separate topic instances that
            // we can subscribe to.
            int numberOfChildrenPerNode = (int)Math.Ceiling((double)requiredLeaves / leaves.Count);

            // Get the number of leaves we need to create in order to have enough
            int totalNumberRemaining = numberOfLevels == 2 ? requiredLeaves : requiredLeaves - leaves.Count;

            // Go through each current leaf create children (the new leaves) as required, until we have enough leaves to support the required number of topic subscribers
            leaves.ForEach(n =>
            {
                if (totalNumberRemaining > 0)
                {
                    // Create new children, thereby creating new leaves. Minimum 2 children must be created, as creating one child will not add
                    // any new leaves.
                    int children = (int)Math.Max(2, Math.Min(totalNumberRemaining, numberOfChildrenPerNode));

                    // Give each node at the second-last level an equal share of children.
                    this.FillWithChildTopics(n, children, 1, 2);

                    // The number of new leaves created is the number of children - 1, since this node is no longer a leaf.
                    int newLeavesCreated = children - 1;

                    totalNumberRemaining -= newLeavesCreated;
                }
            });

            // Copy the total number of nodes in.
            this.TotalNumberOfNodes = this.rootNode.TotalNumberOfNodes;
        }

        /// <summary>
        /// Get the number of leaf topics (the topics that subscriptions can be added to)
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfLeafTopics()
        {
            return this.rootNode.FlattenNodes().Count(p => p.IsLeaf);
        }

        /// <summary>
        /// Ensure the topology exists. If it doesn't, it will be created and auto-forwarding of topic subscriptions set up where relevant.
        /// Any existing topics/subscriptions will be updated to ensure they have the properties required (e.g. auto-forward settings)
        /// </summary>
        public void Emit()
        {
            this.numberOfSubscriptionsProcessed = 0;
            this.numberOfTopicsProcessed = 0;

            this.Report("Creating bus if it doesn't exist");
            this.EmitBusIfNotExists();
            this.Report("Bus created...");
            this.Report("Emitting topics");

            // Emit all topics
            this.rootNode.TraverseParallel(this.EmitTopic);
            this.Report("Topics created!");
            this.Report("Replicating connection string to topology.");

            // Get a connection string for the namespace and pass that to all nodes
            string namespaceConnectionString = this.management.GetNamespaceConnectionString(this.rootNamespaceName);
            this.rootNode.Traverse(p => p.PrimaryConnectionString = namespaceConnectionString);

            // Emit all subscriptions (for auto-forwarding)
            this.EmitSubscriptions();
        }

        /// <summary>
        /// Destroy the bus topology (topics and subscriptions)
        /// </summary>
        /// <param name="leaveRoot"></param>
        public void Destroy(bool leaveRoot = true)
        {
            this.Report("Destroying topology!");

            this.numberOfSubscriptionsProcessed = 0;
            this.numberOfTopicsProcessed = 0;

            if (leaveRoot)
            {
                this.Report("Will leave the root topic...");
                // Only destroy child topics, so iterate child nodes
                foreach (var rootNodeChild in this.rootNode.Children)
                {
                    rootNodeChild.TraverseParallel(this.DestroyTopic);
                }
            }
            else
            {
                this.Report("Will delete the root topic as well...");
                this.rootNode.TraverseParallel(this.DestroyTopic);
            }
        }

        /// <summary>
        /// Create subscriptions for level in the topology, meaning that if there are e.g. two levels (root and x children) the root will have subscriptions that auto-forward
        /// to the same topic on each of the x children.
        /// </summary>
        internal void EmitSubscriptions()
        {
            this.Report("Emitting subscriptions!");

            if (this.rootNode.Children.Count == 0)
            {
                // There are no children, so no forwarding is neccessary
                return;
            }

            this.EmitChildSubscriptions(this.rootNode);
        }

        /// <summary>
        /// Destroy all subscriptions
        /// </summary>
        internal void DestroySubscriptions()
        {
            if (this.rootNode.Children.Count == 0)
            {
                return;
            }

            this.DestroyChildSubscriptions(this.rootNode);
        }

        /// <summary>
        /// Create child subscriptions on the given topic node
        /// </summary>
        /// <param name="node"></param>
        internal void EmitChildSubscriptions(TreeNode<AzureServiceBusTopicInfo> node)
        {
            NamespaceManager namespaceManager =
                NamespaceManager.CreateFromConnectionString(node.Value.PrimaryConnectionString);

            foreach (var child in node.Children)
            {
                // Create a subscription that forwards to the node's child.
                SubscriptionDescription subscription = new SubscriptionDescription(node.Value.Name, child.Value.Name);
                subscription.ForwardTo = child.Value.Name;

                Retry.Do(() =>
                {
                    try
                    {
                        namespaceManager.CreateSubscription(subscription);
                        this.numberOfSubscriptionsProcessed++;
                        this.Report($"\rCreated {this.numberOfSubscriptionsProcessed} of {this.TotalNumberOfNodes} subscriptions");
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })
                .WithDelayInMs(MillisecondDelayBetweenAttemptsOnFailure)
                .MaxTimes(NumberOfRetriesOnFailure)
                .Go();

                this.EmitChildSubscriptions(child);
            }
        }

        /// <summary>
        /// Delete all child subscriptions from the topic
        /// </summary>
        /// <param name="node"></param>
        internal void DestroyChildSubscriptions(TreeNode<AzureServiceBusTopicInfo> node)
        {
            NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(node.Value.PrimaryConnectionString);

            foreach (var child in node.Children)
            {
                Retry.Do(() =>
                {
                    try
                    {
                        namespaceManager.DeleteSubscription(node.Value.Name, "forward-" + child.Value.Name);
                        this.numberOfSubscriptionsProcessed++;
                        this.Report($"Deleted {this.numberOfSubscriptionsProcessed} of {this.TotalNumberOfNodes}");
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })
                    .WithDelayInMs(NumberOfRetriesOnFailure)
                    .MaxTimes(NumberOfRetriesOnFailure).Go();

                this.DestroyChildSubscriptions(child);
            }
        }

        /// <summary>
        /// Create the bus namespace if it doesn't already exist.
        /// </summary>
        internal void EmitBusIfNotExists()
        {
            this.Report("Creating namespace if it doesn't already exist...");
            var existingBus = this.management.GetNamespace(this.rootNamespaceName);

            if (existingBus == null)
            {
                this.management.CreateOrUpdateNamespace(this.rootNamespaceName, this.location);
            }
        }

        /// <summary>
        /// Create a topic through the management api.
        /// </summary>
        /// <param name="topicInfo"></param>
        internal void EmitTopic(AzureServiceBusTopicInfo topicInfo)
        {
            Retry.Do(() =>
            {
                try
                {
                    this.management.CreateOrUpdateTopic(this.rootNamespaceName, topicInfo.Name);
                    this.numberOfTopicsProcessed++;

                    this.Report($"\rCreated {this.numberOfTopicsProcessed} of {this.TotalNumberOfNodes} topics");
                    return true;
                }
                catch (Exception ex)
                {
                    this.Report($"Exception when emitting topic {topicInfo.Name}: {ex}");
                    return false;
                }
            }).WithDelayInMs(MillisecondDelayBetweenAttemptsOnFailure)
                .MaxTimes(NumberOfRetriesOnFailure)
                .Go();
        }

        /// <summary>
        /// Delete a topic.
        /// </summary>
        /// <param name="topicInfo"></param>
        internal void DestroyTopic(AzureServiceBusTopicInfo topicInfo)
        {
            Retry.Do(() =>
            {
                try
                {
                    this.management.DeleteTopic(this.rootNamespaceName, topicInfo.Name);
                    this.numberOfTopicsProcessed++;
                    this.Report($"Deleted {this.numberOfTopicsProcessed} of {this.TotalNumberOfNodes} topics");
                    return true;
                }
                catch (Exception ex)
                {
                    this.Report($"Exception when destroying topic {topicInfo.Name}, {ex}");
                    return false;
                }
            })
            .WithDelayInMs(MillisecondDelayBetweenAttemptsOnFailure)
            .MaxTimes(NumberOfRetriesOnFailure)
            .Go();
        }

        /// <summary>
        /// Create child topics recursively
        /// </summary>
        /// <param name="node"></param>
        /// <param name="numberOfChildren"></param>
        /// <param name="currentLevel"></param>
        /// <param name="maxLevel"></param>
        internal void FillWithChildTopics(TreeNode<AzureServiceBusTopicInfo> node, int numberOfChildren, int currentLevel = 0, int maxLevel = 1)
        {
            if (currentLevel == maxLevel)
            {
                return;
            }

            this.Report($"Will create {numberOfChildren} child topics/subscriptions to {node.Value.Name} (not actual, just topology)");

            for (int i = 1; i <= numberOfChildren; i++)
            {
                var child = new TreeNode<AzureServiceBusTopicInfo>(new AzureServiceBusTopicInfo()
                {
                    Name = $"{node.Value.Name}-{i}"
                });

                if (currentLevel != maxLevel)
                {
                    this.FillWithChildTopics(child, numberOfChildren, currentLevel + 1, maxLevel);
                }

                node.AddChild(child);
            }
        }

        /// <summary>
        /// Report something on the reporting delegate if it is not null
        /// </summary>
        /// <param name="progress"></param>
        internal void Report(string progress)
        {
            this.reportAction?.Invoke(progress);
        }
    }
}