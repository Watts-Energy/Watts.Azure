namespace Watts.Azure.Tests.UnitTests
{
    using System;
    using Common;
    using Common.Interfaces.ServiceBus;
    using Common.ServiceBus.Management;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    /// <summary>
    /// Tests the creation of service bus topology
    /// </summary>
    [TestFixture]
    public class ServiceBusTopologyUnitTests
    {
        private Mock<IAzureServiceBusManagement> mockServiceBusManagement;
        private AzureServiceBusTopology topology;
        private int subscribersPerTopicLimit;

        /// <summary>
        /// Initialize the
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.mockServiceBusManagement = new Mock<IAzureServiceBusManagement>();
            this.subscribersPerTopicLimit = 200;
            this.topology = new AzureServiceBusTopology("busName", "topicName", this.mockServiceBusManagement.Object, AzureLocation.AustraliaCentral, this.subscribersPerTopicLimit);
        }

        /// <summary>
        /// Test that the topology class generates the right topology when the number of subscriptions exceed the limit on the number of subscribers
        /// </summary>
        [Category("UnitTest"), Category("AzureBusTopology")]
        [Test]
        public void Topology_FewerSubscribersThanLimit_CreatesOnlyOneLevel()
        {
            // Set a number of subscriptions that is larger than the allowed number of subscriptions per topic.
            int numberOfTopicSubscriptions = this.subscribersPerTopicLimit / 2;

            this.topology.GenerateBusTopology(numberOfTopicSubscriptions);

            this.topology.GetNumberOfLeafTopics().Should()
                .Be((int)Math.Ceiling((double)numberOfTopicSubscriptions / this.subscribersPerTopicLimit), $"because it should have a number of leaf topics that allow for at least {numberOfTopicSubscriptions} subscriptions with a limit on topic subscriptions set higher than that ({this.subscribersPerTopicLimit}), i.e. 1 topic");
        }

        /// <summary>
        /// Test that the topology class generates the right topology when the number of subscriptions exceed the limit on the number of subscribers
        /// </summary>
        [Category("UnitTest"), Category("AzureBusTopology")]
        [Test]
        public void Topology_CreateEnoughAgentsToCauseScaling_CreatesChildNamespaces_OneSubLevel()
        {
            // Set a number of subscriptions that is larger than the allowed number of subscriptions per topic.
            int numberOfTopicSubscriptions = 3 * this.subscribersPerTopicLimit;

            this.topology.GenerateBusTopology(numberOfTopicSubscriptions);

            this.topology.GetNumberOfLeafTopics().Should()
                .Be((int)Math.Ceiling((double)numberOfTopicSubscriptions / this.subscribersPerTopicLimit), "because the limit on number of subscriptions per topic is less than the number of subscriptions needed, which should lead to the creation of child subscriptions");
        }

        /// <summary>
        /// Tests that when there are more topics than the allowed number of subscriptions per topic squared, the next level is created (i.e. three levels)
        /// </summary>
        [Category("UnitTest"), Category("AzureBusTopology")]
        [Test]
        public void Topology_CreateEnoughAgentsToCauseScaling_CreatesChildNamespaces_TwoLevels()
        {
            // Set a very high number of subscriptions, leaading to three levels total
            int numberOfTopicSubscriptions = (int)Math.Pow(this.subscribersPerTopicLimit, 2) + this.subscribersPerTopicLimit;

            this.topology.GenerateBusTopology(numberOfTopicSubscriptions);

            int numberOfLeafNodes = this.topology.GetNumberOfLeafTopics();

            numberOfLeafNodes.Should()
                .Be((int)Math.Ceiling((double)numberOfTopicSubscriptions / this.subscribersPerTopicLimit), $"because we've requested support for {numberOfTopicSubscriptions} subscriptions with a limit on number of individual topic instance subscriptions of {this.subscribersPerTopicLimit}");
        }
    }
}