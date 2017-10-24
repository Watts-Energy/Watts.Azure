namespace Watts.Azure.Common.ServiceBus.Objects
{
    /// <summary>
    /// Information about a service bus topic
    /// </summary>
    public class AzureServiceBusTopicInfo
    {
        public string Name { get; set; }

        public string PrimaryConnectionString { get; set; }
    }
}