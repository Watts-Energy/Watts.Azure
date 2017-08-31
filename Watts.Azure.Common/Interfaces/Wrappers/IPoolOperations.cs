namespace Watts.Azure.Common.Interfaces.Wrappers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// Interface for Microsoft.Azure.Batch.PoolOperations in order to make it mockable in unit tests.
    /// </summary>
    public interface IPoolOperations
    {
        Task DeletePoolAsync(string poolId, IEnumerable<BatchClientBehavior> additionalBehaviors = null, CancellationToken cancellationToken = default(CancellationToken));

        CloudPool CreatePool(string poolId, string virtualMachineSize, CloudServiceConfiguration cloudServiceConfiguration, int? targetDedicated = default(int?));

        CloudPool CreatePool(string poolId, string virtualMachineSize, VirtualMachineConfiguration virtualMachineConfiguration, int? targetDedicated = default(int?));

        IPagedEnumerable<NodeAgentSku> ListNodeAgentSkus(DetailLevel detailLevel = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null);
    }
}