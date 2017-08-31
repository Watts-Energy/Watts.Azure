namespace Watts.Azure.Utils.Build
{
    using Common.Batch.Objects;
    using Exceptions;
    using Watts.Azure.Utils.Helpers.Batch;
    using Watts.Azure.Utils.Interfaces.Batch;

    public class BatchBuilder
    {
        public IPredefinedBatchEnvironment PredefinedEnvironment { get; set; }

        /// <summary>
        /// Specify batch account settings when executing the batch.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static IBatchCreationWithAccountInfo UsingAccountSettings(BatchAccountSettings account)
        {
            return new BatchCreationWithBatchAccountInfo(account);
        }

        /// <summary>
        /// Specify a predefined environment which already contains the required storage account settings.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static IBatchCreationWithBatchAndStorageAccountSettings InPredefinedEnvironment(
            IPredefinedBatchEnvironment environment)
        {
            if (!environment.IsValid())
            {
                throw new InvalidPredefinedEnvironmentException("The environment passed to BatchBuilder.InPredefinedEnvironment is missing one or more settings. Please check and ensure that it is valid.");
            }

            var retVal = BatchBuilder.UsingAccountSettings(environment.BatchAccountSettings)
                    .UsingStorageAccountSettings(environment.BatchStorageAccountSettings);

            retVal.PredefinedEnvironment = environment;

            return retVal;
        }

        /// <summary>
        /// Skip all the uneccessary steps if this is to be run in a Hybrid batch and it's not the first one (in which case its settings
        /// don't really matter, except for dependency resolution and command setups.
        /// This skips to the step where you are expected to set up which executable/R script is run.
        /// </summary>
        /// <param name="machineConfig"></param>
        /// <returns></returns>
        public static IBatchCreationWithInputPreparation AsNonPrimaryBatch(AzureMachineConfig machineConfig)
        {
            var retVal = new BatchCreationWithInputPreparation(null, null);

            retVal.ConfigureMachines(machineConfig);

            return retVal;
        }
    }
}