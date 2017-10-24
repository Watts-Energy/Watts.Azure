namespace Watts.Azure.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Common;
    using Common.General;
    using Common.Interfaces.Security;
    using Common.Security;
    using Common.ServiceBus.Objects;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Objects;

    /// <summary>
    /// Tests the creation of service bus topology
    /// </summary>
    [TestClass]
    public class ServiceBusTopologyTests
    {
        private TestEnvironmentConfig config;

        private string serviceBusNamespaceName;

        private IAzureActiveDirectoryAuthentication auth;

        /// <summary>
        /// Initialize the 
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.serviceBusNamespaceName = this.config.ServiceBusEnvironment.NamespaceName;
            var credentials = this.config.ServiceBusEnvironment.Credentials;
            this.auth = new AzureActiveDirectoryAuthentication(this.config.ServiceBusEnvironment.SubscriptionId, this.config.ServiceBusEnvironment.ResourceGroupName, credentials);
        }


        /// <summary>
        /// Test that the topology class generates the right topology when the number of subscriptions exceed the limit on the number of subscribers
        /// </summary>
        [TestCategory("UnitTest"), TestCategory("AzureBusTopology")]
        [TestMethod]
        public void Topology_FewerSubscribersThanLimit_CreatesOnlyOneLevel()
        {
            // Set a number of subscriptions that is larger than the allowed number of subscriptions per topic.
            int numberOfTopicSubscriptions = 200;
            int azureSubscriptionLimit = 1000;
            string topicName = "testTopic1";

            AzureServiceBusTopology topology = this.GetTopologyInstance(topicName, azureSubscriptionLimit);

            topology.GenerateBusTopology(numberOfTopicSubscriptions);

            topology.GetNumberOfLeafTopics().Should()
                .Be((int)Math.Ceiling((double) numberOfTopicSubscriptions / azureSubscriptionLimit));
        }

        /// <summary>
        /// Test that the topology class generates the right topology when the number of subscriptions exceed the limit on the number of subscribers
        /// </summary>
        [TestCategory("UnitTest"), TestCategory("AzureBusTopology")]
        [TestMethod]
        public void Topology_CreateEnoughAgentsToCauseScaling_CreatesChildNamespaces_OneSubLevel()
        {
            // Set a number of subscriptions that is larger than the allowed number of subscriptions per topic.
            int numberOfTopicSubscriptions = 600;
            int azureSubscriptionLimit = 100;
            string topicName = "testTopic1";

            AzureServiceBusTopology topology = this.GetTopologyInstance(topicName, azureSubscriptionLimit);

            topology.GenerateBusTopology(numberOfTopicSubscriptions);

            (numberOfTopicSubscriptions / azureSubscriptionLimit).Should().Be(topology.GetNumberOfLeafTopics());
        }

        /// <summary>
        /// Tests that when there are more topics than the allowed number of subscriptions per topic squared, the next level is created (i.e. three levels)
        /// </summary>
        [TestCategory("UnitTest"), TestCategory("AzureBusTopology")]
        [TestMethod]
        public void Topology_CreateEnoughAgentsToCauseScaling_CreatesChildNamespaces_TwoLevels()
        {
            // Set 1500 subscriptions, meaning that there should be three levels total, since only 10000 (100^2) can be supported on two levels
            int numberOfTopicSubscriptions = 15000;
            int azureSubscriptionLimit = 100;

            string topicName = "testTopic2";

            AzureServiceBusTopology topology = this.GetTopologyInstance(topicName, azureSubscriptionLimit);

            topology.GenerateBusTopology(numberOfTopicSubscriptions);

            int numberOfLeafNodes = topology.GetNumberOfLeafTopics();

            (numberOfTopicSubscriptions / azureSubscriptionLimit).Should().Be(numberOfLeafNodes);
        }

        [TestCategory("IntegrationTest"), TestCategory("AzureBusTopology")]
        [TestMethod]
        public void Topology_Emit_CreatesTopology()
        {
            int numberOfTopicSubscriptions = 300;
            int azureSubscriptionLimit = 100;
            string topicName = "integrationtest2";

            AzureServiceBusManagement managementClient = new AzureServiceBusManagement(this.auth);
            this.ClearTopics(managementClient, topicName);

            AzureServiceBusTopology topology = this.GetTopologyInstance(topicName, azureSubscriptionLimit);
            topology.ReportOn(p => Trace.WriteLine(p));
            topology.GenerateBusTopology(numberOfTopicSubscriptions);
            topology.Emit();

            var topics = managementClient.GetTopics(this.serviceBusNamespaceName, topicName);

            topics.Count.Should().Be((numberOfTopicSubscriptions / azureSubscriptionLimit) + 1);

            // Clean up (but leave the root)
            topology.Destroy(true);
        }

        [TestCategory("IntegrationTest"), TestCategory("AzureBusTopology"), TestCategory("LongRunning")]
        [TestMethod]
        public void Topology_ThreeLevels_CreatedCorrectly()
        {
            // ARRANGE
            int numberOfTopicSubscriptions = 200;
            int azureSubscriptionLimit = 10;
            string topicName = "integrationtest";
            AzureServiceBusManagement managementClient = new AzureServiceBusManagement(this.auth);

            // ACT
            // Delete all topics that match the pattern before starting the test.
            this.ClearTopics(managementClient, topicName);

            // In total, there must be at least 20 subscriptions on which to subscribe, as there can be 10 subscribers on each.
            // The way it is created means that there will be 10 children of the root, each of which must have 2 children
            AzureServiceBusTopology topology = this.GetTopologyInstance(topicName, azureSubscriptionLimit);
            topology.ReportOn(p => Trace.WriteLine(p));
            topology.GenerateBusTopology(numberOfTopicSubscriptions);
            topology.Emit();

            // Get all topics which have no subscriptions (i.e. leaf topics in the topology).
            var topics = managementClient.GetTopics(this.serviceBusNamespaceName, topicName).Where(p => p.SubscriptionCount != null && p.SubscriptionCount == 0).ToList();

            // ASSERT that the number of topics equal the expected. There are 200 subscribers and the limit on number of subscriptions is
            // 10, so there must be 20 topics to subscribe to.
            topics.Count.Should().Be((int) (numberOfTopicSubscriptions / azureSubscriptionLimit));

            topology.Destroy();
        }

        internal void ClearTopics(AzureServiceBusManagement managementClient, string topicPattern)
        {
            // Delete all topics that match the pattern before starting the test.
            // There seems to be some instability in the azure service bus management library, so it is neccessary to retry if the operation fails
            Retry.Do(() =>
            {
                try
                {
                    managementClient.DeleteTopics(this.serviceBusNamespaceName, topicPattern, progress => Trace.WriteLine(progress));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            })
            .WithDelayInMs(1000).MaxTimes(5).Go();
        }

        internal AzureServiceBusTopology GetTopologyInstance(string topicName, int numberOfSubscriptionsLimit = 2000)
        {
            return new AzureServiceBusTopology(this.serviceBusNamespaceName, topicName, this.auth, AzureLocation.NorthEurope, numberOfSubscriptionsLimit);
        }
    }
}