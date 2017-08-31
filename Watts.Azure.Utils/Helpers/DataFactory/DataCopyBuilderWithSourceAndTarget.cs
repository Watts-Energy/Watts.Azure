namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using System;
    using Common.DataFactory.Copy;
    using Common.Interfaces.Storage;

    public class DataCopyBuilderWithSourceAndTarget : DataCopyBuilderWithSource
    {
        private Action<string> progressDelegate;

        public DataCopyBuilderWithSourceAndTarget(DataCopyBuilderWithSource parent, IAzureTableStorage targetTable) : base(parent, parent.SourceTable, parent.SourceQuery)
        {
            this.TargetTable = targetTable;
        }

        public IAzureTableStorage TargetTable { get; set; }

        public bool CleanUpAfter { get; set; } = true;

        public DataCopyBuilderWithSourceAndTarget DoNotCleanUpAfter()
        {
            this.CleanUpAfter = false;
            return this;
        }

        public DataCopyBuilderWithSourceAndTarget ReportProgressToConsole()
        {
            this.progressDelegate = Console.WriteLine;
            return this;
        }

        public void StartCopy()
        {
            CopyDataPipeline pipeline = CopyDataPipeline.UsingDataFactorySettings(this.DataFactorySetup, this.CopySetup, this.Authenticator, this.progressDelegate);

            pipeline.FromTable(this.SourceTable).To(this.TargetTable).UsingSourceQuery(this.SourceQuery).Start();

            if (this.CleanUpAfter)
            {
                pipeline.CleanUp();
            }
        }
    }
}