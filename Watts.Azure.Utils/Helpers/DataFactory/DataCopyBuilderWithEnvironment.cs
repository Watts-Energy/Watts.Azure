namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.DataFactory.General;
    using Watts.Azure.Utils.Interfaces.DataFactory;

    public class DataCopyBuilderWithEnvironment
    {
        public DataCopyBuilderWithEnvironment(IPredefinedDataCopyEnvironment environment)
        {
            this.Environment = environment;
        }

        public IPredefinedDataCopyEnvironment Environment { get; set; }

        public DataCopyBuilderWithDataFactorySetup UsingDataFactorySetup(AzureDataFactorySetup dataFactorySetup)
        {
            return new DataCopyBuilderWithDataFactorySetup(this, dataFactorySetup);
        }
    }
}