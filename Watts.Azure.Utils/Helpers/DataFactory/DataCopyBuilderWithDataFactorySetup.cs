namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.DataFactory.Copy;
    using Common.DataFactory.General;

    public class DataCopyBuilderWithDataFactorySetup : DataCopyBuilderWithEnvironment
    {
        public DataCopyBuilderWithDataFactorySetup(DataCopyBuilderWithEnvironment parent, AzureDataFactorySetup dataFactorySetup) : base(parent.Environment)
        {
            this.DataFactorySetup = dataFactorySetup;
        }

        public AzureDataFactorySetup DataFactorySetup { get; set; }

        public DataCopyBuilderWithCopySetup UsingDefaultCopySetup()
        {
            CopySetup setup = new CopySetup()
            {
                SourceDatasetName = "SourceDataset",
                SourceLinkedServiceName = "SourceLinkedService",
                TargetLinkedServiceName = "TargetLinkedService",
                TargetDatasetName = "TargetDataset",
                TimeoutInMinutes = 60,
            };

            return new DataCopyBuilderWithCopySetup(this, setup);
        }

        public DataCopyBuilderWithCopySetup UsingCopySetup(CopySetup setup)
        {
            return new DataCopyBuilderWithCopySetup(this, setup);
        }
    }
}