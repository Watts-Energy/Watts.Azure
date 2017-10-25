namespace Watts.Azure.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Common;
    using Common.General;
    using Common.Interfaces.Security;
    using Common.Security;
    using Common.ServiceBus.Management;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Objects;

    [TestClass]
    public class ServiceBusTopologyIntegrationTests
    {
        private TestEnvironmentConfig config;

        private string serviceBusNamespaceName;

        private IAzureActiveDirectoryAuthentication auth;

        [TestInitialize]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            this.serviceBusNamespaceName = this.config.ServiceBusEnvironment.NamespaceName;
            var credentials = this.config.ServiceBusEnvironment.Credentials;
            this.auth = new AzureActiveDirectoryAuthentication(this.config.ServiceBusEnvironment.SubscriptionId, this.config.ServiceBusEnvironment.ResourceGroupName, credentials);
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

            var topics = managementClient.GetTopics(this.serviceBusNamespaceName, topicName).Where(p => p.SubscriptionCount != null && p.SubscriptionCount == 0).ToList();

            topics.Count.Should().Be((int)Math.Ceiling((double)numberOfTopicSubscriptions / azureSubscriptionLimit), "because we needed enough subscriptions to support 300, but with a limit of 100 on each topic");

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
            topics.Count.Should().Be((int)Math.Ceiling((double)numberOfTopicSubscriptions / azureSubscriptionLimit), $"because we've asked for {numberOfTopicSubscriptions} subscriptions but only support {azureSubscriptionLimit} per topic");

            topology.Destroy();
        }

        /// <summary>
        /// Delete all topics whose name starts with the given pattern.
        /// </summary>
        /// <param name="managementClient"></param>
        /// <param name="topicPattern"></param>
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
            return new AzureServiceBusTopology(this.serviceBusNamespaceName, topicName, new AzureServiceBusManagement(this.auth), AzureLocation.NorthEurope, numberOfSubscriptionsLimit);
        }
    }
}