namespace Watts.Azure.Utils.Build
{
    using Watts.Azure.Utils.Helpers.DataFactory;
    using Watts.Azure.Utils.Interfaces.DataFactory;

    public class DataCopyBuilder
    {
        public static DataCopyBuilderWithEnvironment InDataFactoryEnvironment(IPredefinedDataCopyEnvironment environment)
        {
            return new DataCopyBuilderWithEnvironment(environment);
        }
    }
}