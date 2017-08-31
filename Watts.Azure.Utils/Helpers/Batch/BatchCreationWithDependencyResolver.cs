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
                ? new List<IDependencyResolver>()
                : parent.DependencyResolvers;
        }

        public BatchCreationWithDependencyResolver(BatchCreationWithBatchAndStorageAccountSettings parent, IDependencyResolver dependencyResolver)
            : base(parent, parent.StorageAccountSettings)
        {
            this.DependencyResolvers = new List<IDependencyResolver>() { dependencyResolver };
        }

        public BatchCreationWithDependencyResolver(BatchCreationWithBatchAndStorageAccountSettings parent, List<IDependencyResolver> dependencyResolvers)
            : base(parent, parent.StorageAccountSettings)
        {
            this.DependencyResolvers = new List<IDependencyResolver>() { };
            this.DependencyResolvers.AddRange(dependencyResolvers);
        }

        public List<IDependencyResolver> DependencyResolvers { get; set; }

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