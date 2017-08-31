namespace Watts.Azure.Common.DataFactory.Copy
{
    using System;
    using System.Linq;
    using System.Threading;
    using General;
    using Interfaces.Security;
    using Interfaces.Storage;

    /// <summary>
    /// A copy data pipeline that copies data from a source table to a target table.
    /// </summary>
    public class CopyDataPipeline
    {
        private readonly CopyTableSetup setup;
        private readonly AzureDataFactoryTableCopy dataFactory;

        /// <summary>
        /// A progress delegate that we can report status on.
        /// </summary>
        private readonly Action<string> progressDelegate;

        private IAzureTableStorage sourceTable;
        private IAzureTableStorage targetTable;

        /// <summary>
        /// Instantiate a copy data pipeline to replicate data from one table to another table.
        /// </summary>
        /// <param name="factorySetup"></param>
        /// <param name="setup"></param>
        /// <param name="authentication"></param>
        /// <param name="progressDelegate"></param>
        private CopyDataPipeline(AzureDataFactorySetup factorySetup, CopyTableSetup setup, IAzureActiveDirectoryAuthentication authentication, Action<string> progressDelegate = null)
        {
            this.setup = setup;
            this.progressDelegate = progressDelegate;
            this.dataFactory = new AzureDataFactoryTableCopy(factorySetup, setup, authentication, progressDelegate);
        }

        public static CopyDataPipeline UsingDataFactorySettings(AzureDataFactorySetup setup, CopyTableSetup copySetup, IAzureActiveDirectoryAuthentication authentication, Action<string> progressDelegate = null)
        {
            return new CopyDataPipeline(setup, copySetup, authentication, progressDelegate);
        }

        public CopyDataPipeline FromTable(IAzureTableStorage source)
        {
            this.sourceTable = source;
            return this;
        }

        public CopyDataPipeline To(IAzureTableStorage target)
        {
            this.targetTable = target;
            return this;
        }

        public CopyDataPipeline UsingSourceQuery(string sourceQuery)
        {
            this.dataFactory.UsingSourceQuery(sourceQuery);
            return this;
        }

        public void Start()
        {
            var existingPipeline = this.dataFactory.PipelineExists();
            if (existingPipeline != null)
            {
                this.Report("Pipeline already exists. Monitoring...");
                this.dataFactory.MonitorStatusUntilDone(this.setup.TargetDatasetName, existingPipeline.Properties.Start.Value, existingPipeline.Properties.End.Value);
            }

            if (this.setup.CreateTargetTableIfNotExists)
            {
                this.CreateTargetTableIfNotExists();
            }

            var tableStructure = this.sourceTable.GetTableStructure();

            this.dataFactory.CreateDataFactory();

            this.dataFactory.LinkService(this.sourceTable.ConnectionString, this.setup.SourceLinkedServiceName);
            this.dataFactory.LinkService(this.targetTable.ConnectionString, this.setup.TargetLinkedServiceName);

            this.dataFactory.CreateDatasets(this.sourceTable.TableName, this.targetTable.TableName, tableStructure);

            DateTime pipelineActivePeriodStartTime = DateTime.Now.ToUniversalTime().AddHours(-100);
            DateTime pipelineActivePeriodEndTime = pipelineActivePeriodStartTime.AddMinutes(200);

            this.dataFactory.CreatePipeline(this.setup.SourceDatasetName, this.setup.TargetDatasetName, pipelineActivePeriodStartTime, pipelineActivePeriodEndTime);
            this.dataFactory.MonitorStatusUntilDone(this.setup.TargetDatasetName, pipelineActivePeriodStartTime, pipelineActivePeriodEndTime);

            this.Report("Waiting 20 seconds");
            Thread.Sleep(20000);

            this.dataFactory.PrintRunDetails(this.setup.TargetDatasetName, pipelineActivePeriodStartTime);

            var errors = this.dataFactory.GetErrors(pipelineActivePeriodStartTime);

            Console.WriteLine("Pipeline ran to completion with {0} errors...", errors.Count);

            if (errors.Count > 0)
            {
                throw new AggregateException("There were errors when copying data", errors.Select(p => new Exception(p)));
            }
        }

        public void CleanUp()
        {
            this.dataFactory.Delete();
        }

        internal void CreateTargetTableIfNotExists()
        {
            var tableReference = this.targetTable.GetTableReference();

            if (!tableReference.Exists())
            {
                this.Report($"Target table did not already exist. Creating table {this.targetTable.TableName}");
                var exampleEntities = this.sourceTable.GetTop(10);

                var templateEntity =
                    exampleEntities.FirstOrDefault(
                        e => e.Properties.Count() == exampleEntities.Max(p => p.Properties.Count()));

                this.targetTable.CreateTableFromTemplateEntity(templateEntity);
            }
        }

        internal void Report(string progress)
        {
            this.progressDelegate?.Invoke(progress);
        }
    }
}