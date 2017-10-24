namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.DataFactory.General;
    using Watts.Azure.Utils.Interfaces.DataFactory;

    public class DataCopyBuilderWithEnvironment
    {
        public DataCopyBuilderWithEnvironment(IDataCopyEnvironment environment)
        {
            this.Environment = environment;
        }

        public IDataCopyEnvironment Environment { get; set; }

        public DataCopyBuilderWithDataFactorySetup UsingDataFactorySetup(AzureDataFactorySetup dataFactorySetup)
        {
            return new DataCopyBuilderWithDataFactorySetup(this, dataFactorySetup);
        }
    }
}