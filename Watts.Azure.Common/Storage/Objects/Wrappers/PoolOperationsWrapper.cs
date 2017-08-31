namespace Watts.Azure.Common.Storage.Objects.Wrappers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;

    /// <summary>
    /// A wrapper of Microsoft.Azure.Batch.PoolOperations in order to make it mockable in unit tests.
    /// </summary>
    public class PoolOperationsWrapper : IPoolOperations
    {
        private readonly PoolOperations poolOperations;

        public PoolOperationsWrapper(PoolOperations poolOperations)
        {
            this.poolOperations = poolOperations;
        }

        public async Task DeletePoolAsync(string poolId, IEnumerable<BatchClientBehavior> additionalBehaviors = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.poolOperations.DeletePoolAsync(poolId, additionalBehaviors, cancellationToken);
        }

        public CloudPool CreatePool(string poolId, string virtualMachineSize, CloudServiceConfiguration cloudServiceConfiguration, int? targetDedicated = default(int?))
        {
            return this.poolOperations.CreatePool(poolId, virtualMachineSize, cloudServiceConfiguration, targetDedicated);
        }

        public CloudPool CreatePool(string poolId, string virtualMachineSize, VirtualMachineConfiguration virtualMachineConfiguration, int? targetDedicated = default(int?))
        {
            return this.poolOperations.CreatePool(poolId, virtualMachineSize, virtualMachineConfiguration, targetDedicated);
        }

        public IPagedEnumerable<NodeAgentSku> ListNodeAgentSkus(DetailLevel detailLevel = null, IEnumerable<BatchClientBehavior> additionalBehaviors = null)
        {
            return this.poolOperations.ListNodeAgentSkus(detailLevel, additionalBehaviors);
        }
    }
}