namespace Watts.Azure.Tests
{
    using Common.Interfaces.Wrappers;
    using Common.ServiceBus.Objects;
    using FluentAssertions;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Objects;

    [TestClass]
    public class ServiceBusTopicUnitTests
    {
        private AzureServiceBusTopic topic;
        private string namespaceName;
        private string connectionString;
        private Mock<INamespaceManager> mockNamespaceManager;
        private Mock<ITopicClient> mockTopicClient;
        private Mock<ITopicSubscriptionClient> mockTopicSubscriptionClient;

        [TestInitialize]
        public void Setup()
        {
            this.namespaceName = "testbus";
            this.connectionString = $"Endpoint=sb://{this.namespaceName}.servicebus";
            this.mockNamespaceManager = new Mock<INamespaceManager>();
            this.mockTopicClient = new Mock<ITopicClient>();
            this.mockTopicSubscriptionClient = new Mock<ITopicSubscriptionClient>();
            this.topic = new AzureServiceBusTopic("topicName", "subscriptionId", this.connectionString, this.mockNamespaceManager.Object, this.mockTopicClient.Object, this.mockTopicSubscriptionClient.Object);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void NamespaceNameIsCorrect() => this.topic.NamespaceName.Should().Be(this.namespaceName);

        [TestCategory("UnitTest")]
        [TestMethod]
        public void SendMessage_InvokesTopicClientWithBrokeredMessage_Once()
        {
            object someObject = new TestObject(1, "bla");

            this.topic.SendMessage(someObject).Wait();

            this.mockTopicClient.Verify(p => p.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void SendMessage_InvokesTopicClientWithBrokeredMessage_HasCorrectProperties()
        {
            object someObject = new TestObject(1, "bla");

            this.topic.SendMessage(someObject).Wait();

            // Verify that the brokered message has the properties from the test object
            this.mockTopicClient.Verify(p => p.SendAsync(It.Is<BrokeredMessage>(q => q.Properties.ContainsKey("A") && q.Properties.ContainsKey("B"))));
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionExists_ShouldNotRecreate_DoesNotAttemptToDelete()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            this.topic.CreateSubscriptionIfDoesntExist(false);

            this.mockNamespaceManager.Verify(p => p.DeleteSubscription(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionExists_Recreate_AttemptsToDelete()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.DeleteSubscription(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionDoesNotExists_Recreate_DoesNotDelete()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.DeleteSubscription(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionDeleted_FilterEmpty_InvokesCreateSubscriptionWithoutFilter()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.CreateSubscription(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionDeleted_FilterEmpty_DoesNotInvokesCreateSubscriptionWithFilter()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.CreateSubscription(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Filter>()), Times.Never);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionDeleted_FilterIsSet_DoesNotInvokesCreateSubscriptionWithoutFilter()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            this.topic.SetFilter("some filter");
            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.CreateSubscription(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateSubscriptionIfDoesntExist_SubscriptionDeleted_FilterIsSet_InvokesCreateSubscriptionWithFilter()
        {
            this.mockNamespaceManager.Setup(p => p.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            this.topic.SetFilter("some filter");
            this.topic.CreateSubscriptionIfDoesntExist(true);

            this.mockNamespaceManager.Verify(p => p.CreateSubscription(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Filter>()), Times.Once);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateTopicIfNotExists_TopicExists_DoesNotInvokeCreate()
        {
            this.mockNamespaceManager.Setup(p => p.TopicExists(It.IsAny<string>())).Returns(true);

            this.topic.CreateTopicIfNotExists();

            this.mockNamespaceManager.Verify(p => p.CreateTopic(It.IsAny<TopicDescription>()), Times.Never);
        }

        [TestCategory("UnitTest")]
        [TestMethod]
        public void CreateTopicIfNotExists_TopicDoesntExists_InvokesCreateOnce()
        {
            this.mockNamespaceManager.Setup(p => p.TopicExists(It.IsAny<string>())).Returns(false);

            this.topic.CreateTopicIfNotExists();

            this.mockNamespaceManager.Verify(p => p.CreateTopic(It.IsAny<TopicDescription>()), Times.Once);
        }
    }
}