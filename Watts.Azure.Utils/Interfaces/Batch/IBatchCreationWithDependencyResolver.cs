namespace Watts.Azure.Utils.Interfaces.Batch
{
    using Common.Batch.Objects;

    public interface IBatchCreationWithDependencyResolver
    {
        IBatchCreationWithPoolSetup WithDefaultPoolSetup();

        IBatchCreationWithPoolSetup WithPoolSetup(BatchPoolSetup poolSetup);
    }
}