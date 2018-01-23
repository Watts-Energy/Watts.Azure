namespace Watts.Azure.Common.ServiceBus.Objects
{
    /// <summary>
    /// Information about a service bus topic
    /// </summary>
    public class AzureServiceBusTopicInfo
    {
        public AzureServiceBusTopicInfo()
        {
            
        }

        public AzureServiceBusTopicInfo(string name, string primaryConnectionString)
        {
            this.Name = name;
            this.PrimaryConnectionString = primaryConnectionString;
        }

        public string Name { get; set; }

        public string PrimaryConnectionString { get; set; }
    }
}