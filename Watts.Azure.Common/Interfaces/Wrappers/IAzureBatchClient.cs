namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using System;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// An interface for methods from AzureBatchClient.
    /// The interface exists in order to be able to mock the client in unit tests.
    /// </summary>
    public interface IAzureBatchClient : IDisposable
    {
        IPoolOperations PoolOperations { get; }

        IJobOperations JobOperations { get; }

        Utilities Utilities { get; }
    }
}