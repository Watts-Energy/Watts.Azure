namespace Watts.Azure.Utils.Helpers.Batch
{
    using System.Collections.Generic;
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch account with the dependency resolver specified.
    /// </summary>
    public class BatchCreationWithDependencyResolver : BatchCreationWithBatchAndStorageAccountSettings, IBatchCreationWithDependencyResolver
    {
        public BatchCreationWithDependencyResolver(BatchCreationWithDependencyResolver parent)
           : base(parent)
        {
            this.DependencyResolvers = parent == null
                ? new List<IBatchDependencyResolver>()
                : parent.DependencyResolvers;
        }

        public BatchCreationWithDependencyResolver(BatchCreationWithBatchAndStorageAccountSettings parent, IBatchDependencyResolver dependencyResolver)
            : base(parent, parent.StorageAccountSettings)
        {
            this.DependencyResolvers = dependencyResolver == null ? new List<IBatchDependencyResolver>() : new List<IBatchDependencyResolver>() { dependencyResolver };
        }

        public BatchCreationWithDependencyResolver(BatchCreationWithBatchAndStorageAccountSettings parent, List<IBatchDependencyResolver> dependencyResolvers)
            : base(parent, parent.StorageAccountSettings)
        {
            this.DependencyResolvers = new List<IBatchDependencyResolver>() { };
            this.DependencyResolvers.AddRange(dependencyResolvers);
        }

        public List<IBatchDependencyResolver> DependencyResolvers { get; set; }

        /// <summary>
        /// Use the default pool setup where the JobId is BatchJob and PoolId is BatchPool.
        /// To override the naming, use WithPoolSetup instead.
        /// </summary>
        /// <returns></returns>
        public IBatchCreationWithPoolSetup WithDefaultPoolSetup()
        {
            return new BatchCreationWithPoolSetup(this, new BatchPoolSetup() { JobId = "BatchJob", PoolId = "BatchPool" });
        }

        /// <summary>
        /// Specify the pool name and id to use. To use default settings use WithDefaultPoolSetup instead.
        /// </summary>
        /// <param name="poolSetup">Specification of the id and name for the pool</param>
        /// <returns></returns>
        public IBatchCreationWithPoolSetup WithPoolSetup(BatchPoolSetup poolSetup)
        {
            return new BatchCreationWithPoolSetup(this, poolSetup);
        }
    }
}