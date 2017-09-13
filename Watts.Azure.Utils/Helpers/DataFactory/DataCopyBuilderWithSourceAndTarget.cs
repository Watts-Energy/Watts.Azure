namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using System;
    using Common.DataFactory.Copy;
    using Watts.Azure.Common.Interfaces.DataFactory;

    public class DataCopyBuilderWithSourceAndTarget : DataCopyBuilderWithSource
    {
        private Action<string> progressDelegate;

        public DataCopyBuilderWithSourceAndTarget(DataCopyBuilderWithSource parent, IAzureLinkedService target)
            : base(parent, parent.Source, parent.SourceQuery)
        {
            this.Target = target;
        }

        public IAzureLinkedService Target { get; set; }

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

        public DataCopyBuilderWithSourceAndTarget ReportProgressTo(Action<string> progressAction)
        {
            this.progressDelegate = progressAction;
            return this;
        }

        public void StartCopy()
        {
            CopyDataPipeline pipeline = CopyDataPipeline.UsingDataFactorySettings(this.DataFactorySetup, this.CopySetup, this.Authenticator, this.progressDelegate);

            pipeline.From(this.Source).To(this.Target).UsingSourceQuery(this.SourceQuery).Start();

            if (this.CleanUpAfter)
            {
                pipeline.CleanUp();
            }
        }
    }
}