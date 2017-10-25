namespace Watts.Azure.Common.Interfaces.General
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for a class that resolves dependencies of an executable or script to be executed in Azure batch.
    /// </summary>
    public interface IBatchDependencyResolver
    {
        IEnumerable<string> Resolve();
    }
}