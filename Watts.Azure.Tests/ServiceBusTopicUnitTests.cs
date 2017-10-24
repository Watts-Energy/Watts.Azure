namespace Watts.Azure.Tests
{
    using FluentAssertions;
    using Common.ServiceBus.Objects;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ServiceBusTopicUnitTests
    {
        private AzureServiceBusTopic topic;
        private string namespaceName;
        private string connectionString ;

        [TestInitialize]
        public void Setup()
        {
            this.namespaceName = "testbus";
            this.connectionString = $"Endpoint=sb://{this.namespaceName}.servicebus";
            this.topic= new AzureServiceBusTopic("bla", "bla", this.connectionString);
        }

        [TestMethod]
        public void NamespaceNameIsCorrect() => this.topic.NamespaceName.Should().Be(this.namespaceName);
    }
}