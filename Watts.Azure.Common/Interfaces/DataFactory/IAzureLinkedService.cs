namespace Watts.Azure.Common.Interfaces.DataFactory
{
    /// <summary>
    /// Interface for a Azure data factory linked service.
    /// </summary>
    public interface IAzureLinkedService
    {
        string ConnectionString { get; }
    }
}