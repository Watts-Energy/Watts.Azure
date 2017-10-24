namespace Watts.Azure.Utils.Interfaces.Batch
{
    using System.Collections.Generic;
    using Common.Batch.Objects;
    using Common.Interfaces.General;

    public interface IBatchCreationWithBatchAndStorageAccountSettings : IBatchCreationWithAccountInfo
    {
        IBatchEnvironment PredefinedEnvironment { get; set; }

        IBatchCreationWithBatchAndStorageAccountSettings RunStartupCommandOnAllNodes(BatchConsoleCommand command);

        IBatchCreationWithDependencyResolver ResolveDependenciesUsing(IDependencyResolver dependencyResolver);

        IBatchCreationWithDependencyResolver ResolveDependenciesUsing(List<IDependencyResolver> dependencyResolvers);

        IBatchCreationWithDependencyResolver NoDependencies();
    }
}