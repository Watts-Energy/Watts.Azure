namespace Watts.Azure.Utils.Helpers.Batch
{
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Watts.Azure.Utils.Interfaces.Batch;

    /// <summary>
    /// A batch creation with pool setup specified.
    /// </summary>
    public class BatchCreationWithPoolSetup : BatchCreationWithDependencyResolver, IBatchCreationWithPoolSetup
    {
        public BatchCreationWithPoolSetup(BatchCreationWithDependencyResolver parent, BatchPoolSetup poolSetup)
            : base(parent)
        {
            this.PoolSetup = poolSetup;

            this.MachineConfig = AzureMachineConfig.Small().Instances(2);
        }

        public BatchCreationWithPoolSetup(BatchCreationWithPoolSetup parent)
            : base(parent)
        {
            this.PoolSetup = parent?.PoolSetup;
            this.MachineConfig = parent?.MachineConfig;
        }

        protected AzureMachineConfig MachineConfig { get; set; }

        protected BatchPoolSetup PoolSetup { get; set; }

        public BatchCreationWithPoolSetup ConfigureMachines(AzureMachineConfig machineConfig)
        {
            this.MachineConfig = machineConfig;
            return this;
        }

        public BatchCreationWithPoolSetup WithDefaultMachineConfig()
        {
            this.MachineConfig = AzureMachineConfig.Small().Instances(2);
            return this;
        }

        public BatchCreationWithPoolSetup WithOneSmallMachine()
        {
            this.MachineConfig = AzureMachineConfig.Small().Instances(1);
            return this;
        }

        public BatchCreationWithInputPreparation PrepareInputUsing(IPrepareInputFiles inputPreparer)
        {
            return new BatchCreationWithInputPreparation(this, inputPreparer);
        }
    }
}