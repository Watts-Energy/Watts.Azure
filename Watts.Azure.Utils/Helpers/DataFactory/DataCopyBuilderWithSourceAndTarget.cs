namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Common.DataFactory.Copy;
    using Common.Storage.Objects;
    using Microsoft.Azure.Management.DataFactories.Common.Models;
    using Microsoft.WindowsAzure.Storage.Table;
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

        public DataStructure DataStructure { get; set; }

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

        public DataCopyBuilderWithSourceAndTarget StructuredAs<T>() where T : TableEntity
        {
            var properties = typeof(T).GetProperties();

            this.DataStructure = new DataStructure();

            // Add the default key structure where the partitionkey and rowkey are strings.
            this.DataStructure.AddDefaultKeyStructure(null, null);

            foreach (PropertyInfo prop in properties.Where(p => p.Name.ToLowerInvariant() != "partitionkey" && p.Name.ToLowerInvariant() != "rowkey"))
            {
                this.DataStructure.AddColumn(prop.Name, prop.PropertyType.Name);
            }

            this.CopySetup.DataStructure = this.DataStructure;

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