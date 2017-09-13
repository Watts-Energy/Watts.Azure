namespace Watts.Azure.Common.DataFactory.Copy
{
    using System;
    using System.Linq;
    using System.Threading;
    using General;
    using Interfaces.Security;
    using Interfaces.Storage;
    using Watts.Azure.Common.Interfaces.DataFactory;

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

        private IAzureLinkedService sourceService;
        private IAzureLinkedService targetService;


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

        public CopyDataPipeline FromTable(IAzureLinkedService source)
        {
            this.sourceService = source;
            return this;
        }

        public CopyDataPipeline To(IAzureLinkedService target)
        {
            this.targetService = target;
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
                LinkedServiceHelper helper = new LinkedServiceHelper(this.Report);
                helper.CreateTargetIfItDoesntExist(this.sourceService, this.targetService);
            }

            var tableStructure = this.sourceService.GetStructure();

            this.dataFactory.CreateDataFactory();

            this.dataFactory.LinkService(this.sourceService.ConnectionString, this.setup.SourceLinkedServiceName);
            this.dataFactory.LinkService(this.targetService.ConnectionString, this.setup.TargetLinkedServiceName);

            this.dataFactory.CreateDatasets(this.sourceService.Name, this.targetService.Name, tableStructure);

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

        internal void Report(string progress)
        {
            this.progressDelegate?.Invoke(progress);
        }
    }
}