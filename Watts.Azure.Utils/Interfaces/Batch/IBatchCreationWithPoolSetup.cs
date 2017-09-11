namespace Watts.Azure.Utils.Interfaces.Batch
{
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Microsoft.Azure.Batch.Auth;

    public interface IBatchCreationWithPoolSetup
    {
        BatchSharedKeyCredentials Credentials { get; set; }

        IBatchCreationWithPoolSetup ConfigureMachines(AzureMachineConfig machineConfig);

        IBatchCreationWithPoolSetup WithDefaultMachineConfig();

        IBatchCreationWithPoolSetup WithOneSmallMachine();

        IBatchCreationWithInputPreparation PrepareInputUsing(IPrepareInputFiles inputPreparer);
    }
}