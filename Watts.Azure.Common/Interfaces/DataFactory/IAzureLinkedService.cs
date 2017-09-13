namespace Watts.Azure.Common.Interfaces.DataFactory
{
    using Watts.Azure.Common.Storage.Objects;

    /// <summary>
    /// Interface for a Azure data factory linked service.
    /// </summary>
    public interface IAzureLinkedService
    {
        string Name { get; set; }

        string ConnectionString { get; }

        DataStructure GetStructure(string partitionKeyType = null, string rowKeyType = null);
    }
}