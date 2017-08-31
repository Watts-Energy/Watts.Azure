namespace Watts.Azure.Utils.Interfaces.Batch
{
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Microsoft.Azure.Batch.Auth;
    using Watts.Azure.Utils.Helpers.Batch;

    public interface IBatchCreationWithPoolSetup
    {
        BatchSharedKeyCredentials Credentials { get; set; }

        BatchCreationWithPoolSetup ConfigureMachines(AzureMachineConfig machineConfig);

        BatchCreationWithPoolSetup WithDefaultMachineConfig();

        BatchCreationWithPoolSetup WithOneSmallMachine();

        BatchCreationWithInputPreparation PrepareInputUsing(IPrepareInputFiles inputPreparer);
    }
}