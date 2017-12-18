namespace Watts.Azure.Tests.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Common;
    using Common.General;
    using Common.Interfaces.Security;
    using Common.Security;
    using Common.ServiceBus.Management;
    using Common.ServiceBus.Objects;
    using FluentAssertions;
    using Microsoft.ServiceBus.Messaging;
    using NUnit.Framework;
    using Objects;
    using Constants = Tests.Constants;

    [TestFixture]
    public class ServiceBusTopicIntegrationTests
    {
        private string topicName;
        private string subscriptionName;

        private TestEnvironmentConfig config;
        private IAzureActiveDirectoryAuthentication auth;
        private AzureServiceBusTopic topic;

        [SetUp]
        public void Setup()
        {
            this.config = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment();

            var credentials = this.config.ServiceBusEnvironment.Credentials;
            this.auth = new AzureActiveDirectoryAuthentication(this.config.ServiceBusEnvironment.SubscriptionId, this.config.ServiceBusEnvironment.ResourceGroupName, credentials);

            this.topicName = "some-topic";
            this.subscriptionName = "testSubscription";

            this.topic = new AzureServiceBusTopic(
                this.topicName,
                this.subscriptionName,
                this.config.ServiceBusEnvironment.ServiceBusConnectionString);

            // Create the topic and subscription if they do not exist
            this.topic.CreateIfNotExists(true);

            int clearedMessages = this.topic.ClearMessages();
            Trace.WriteLine($"Cleared {clearedMessages} from the topic subscriber");
        }

        [Test, Category("IntegrationTest")]
        public void SimplePublishSubscribeToTopic()
        {
            BrokeredMessage receivedMessage = null;

            // Create a test object and push that onto the topic
            TestObject testObject = new TestObject(2, "test");

            this.topic.SendMessageAsync(testObject.ToBrokeredMessage()).Wait();

            // Now subscribe to the topic to receive the message
            this.topic.Subscribe(p => receivedMessage = p);

            this.WaitForItToBeNotNull(receivedMessage);

            // Assert that we actually received the brokeredmessage from the bus
            receivedMessage.Should().NotBeNull("because the subscription should receive messages sent on the topic");
        }

        /// <summary>
        /// Tests that when there are multiple subscriptions on one topic, a message delivered on the topic is passed to both subscribers
        /// </summary>
        [Test, Category("IntegrationTest")]
        public void MultipleSubscriptionsToTopic_BothSubscribersReceiveMessage()
        {
            // Create an additional subscription
            AzureServiceBusTopic secondTopicSubscriber = new AzureServiceBusTopic(this.topicName, this.subscriptionName + "-2", this.config.ServiceBusEnvironment.ServiceBusConnectionString);

            // Create the topic subscriber and clear any messages already on there
            secondTopicSubscriber.CreateIfNotExists(false);
            int clearedMessages = secondTopicSubscriber.ClearMessages();
            Trace.WriteLine($"Cleared {clearedMessages} from the second topic subscriber");

            TestObject testMessage = new TestObject(5, "A");

            BrokeredMessage receivedByTopic = null;
            BrokeredMessage receivedBySecondTopic = null;

            this.topic.Subscribe(p => receivedByTopic = p);
            secondTopicSubscriber.Subscribe(p => receivedBySecondTopic = p);

            this.topic.SendMessageAsync(testMessage.ToBrokeredMessage()).Wait();

            // Wait for both topic subscribers to receive the message
            this.WaitForItToBeNotNull(receivedByTopic, receivedBySecondTopic);

            receivedByTopic.Should().NotBeNull("because the first topic subscription should receive the message");
            receivedBySecondTopic.Should().NotBeNull("because the second topic subscription should receive the message");
        }

        [Test, Category("IntegrationTest"), Category("AzureBusTopology")]
        public void MultiLayerTopology_SubsriptionsAtLeaf_ReceivesMessagesSentToRoot()
        {
            int subscribersPerTopic = 5;

            string nameOfTopic = Guid.NewGuid().ToString().Substring(0, 5);

            AzureServiceBusTopology topology = new AzureServiceBusTopology(this.topic.NamespaceName, nameOfTopic, new AzureServiceBusManagement(this.auth), AzureLocation.NorthEurope, subscribersPerTopic);

            // Create three times as many subscribers as will fit in one topic
            topology.GenerateBusTopology(subscribersPerTopic * 3);
            topology.Emit();

            // Create a subscriber on the first leaf
            var firstLeaf = topology.GetLeafNodes().First();
            AzureServiceBusTopic topicSubscriber = new AzureServiceBusTopic(firstLeaf.Value.Name, firstLeaf.Value.Name + "-sub", firstLeaf.Value.PrimaryConnectionString);

            // Ensure there are no messages pending (it might have already existed)
            topicSubscriber.CreateSubscriptionIfDoesntExist(true);
            topicSubscriber.ClearMessages();

            // Subscribe to messages
            BrokeredMessage receivedMessage = null;
            topicSubscriber.Subscribe(p =>
                receivedMessage = p
            );

            // Create the root topic client and push a message onto that.
            var rootTopic = topology.GetRootTopic();
            AzureServiceBusTopic rootTopicClient = new AzureServiceBusTopic(rootTopic.Name, null, rootTopic.PrimaryConnectionString);

            // Send a message onto the root topic of the topology.
            // This should cause the message to be forwarded to all child topics, and then be delivered to the subscription we created above.
            TestObject testMessage = new TestObject(5, "B");

            rootTopicClient.SendMessage(testMessage.ToBrokeredMessage());

            // Wait a little while or until the message was received (i.e. is not null)
            this.WaitForItToBeNotNull(receivedMessage);

            // Clean up
            topology.Destroy(false);

            receivedMessage.Should()
                .NotBeNull("because the leaf topic should receive all messages sent on the root topic in a layered topology");
        }

        internal void WaitForItToBeNotNull(params object[] obj)
        {
            // Wait for the message to be received
            Retry.Do(() => obj == null)
                .MaxTimes(100)
                .WithDelayInMs(500)
                .Go();
        }
    }
}