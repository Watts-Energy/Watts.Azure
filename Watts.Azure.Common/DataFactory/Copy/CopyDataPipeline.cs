namespace Watts.Azure.Common.DataFactory.Copy
{
    using System;
    using System.Linq;
    using System.Threading;
    using General;
    using Interfaces.Security;
    using Watts.Azure.Common.Interfaces.DataFactory;

    /// <summary>
    /// A copy data pipeline that copies data from a source service to a target service, e.g. Azure Table Storage -> Azure Data Lake.
    /// </summary>
    public class CopyDataPipeline
    {
        private readonly CopySetup setup;
        private readonly AzureDataFactoryCopy dataFactory;

        /// <summary>
        /// A progress delegate that we can report status on.
        /// </summary>
        private readonly Action<string> progressDelegate;

        private IAzureLinkedService sourceService;
        private IAzureLinkedService targetService;

        /// <summary>
        /// Instantiate a copy data pipeline to replicate data from one service to another.
        /// </summary>
        /// <param name="factorySetup"></param>
        /// <param name="setup"></param>
        /// <param name="authentication"></param>
        /// <param name="progressDelegate"></param>
        private CopyDataPipeline(AzureDataFactorySetup factorySetup, CopySetup setup, IAzureActiveDirectoryAuthentication authentication, Action<string> progressDelegate = null)
        {
            this.setup = setup;
            this.progressDelegate = progressDelegate;
            this.dataFactory = new AzureDataFactoryCopy(factorySetup, setup, authentication, progressDelegate);
        }

        public static CopyDataPipeline UsingDataFactorySettings(AzureDataFactorySetup setup, CopySetup copySetup, IAzureActiveDirectoryAuthentication authentication, Action<string> progressDelegate = null)
        {
            return new CopyDataPipeline(setup, copySetup, authentication, progressDelegate);
        }

        public CopyDataPipeline From(IAzureLinkedService source)
        {
            this.sourceService = source;
            this.dataFactory.SourceService = this.sourceService;
            return this;
        }

        public CopyDataPipeline To(IAzureLinkedService target)
        {
            this.targetService = target;
            this.dataFactory.TargetService = this.targetService;
            return this;
        }

        public CopyDataPipeline UsingSourceQuery(string sourceQuery)
        {
            this.dataFactory.UsingSourceQuery(sourceQuery);
            return this;
        }

        public void Start()
        {
            if (this.setup.DeleteDataFactoryIfExists)
            {
                this.DeleteDataFactoryIfExists();
            }

            var existingPipeline = this.dataFactory.PipelineExists();
            if (existingPipeline != null)
            {
                this.Report("Pipeline already exists. Monitoring...");
                this.dataFactory.MonitorStatusUntilDone(this.setup.TargetDatasetName,
                    existingPipeline.Properties.Start.Value, existingPipeline.Properties.End.Value);
            }

            if (this.setup.CreateTargetIfNotExists)
            {
                LinkedServiceHelper helper = new LinkedServiceHelper(this.Report);
                helper.CreateTargetIfItDoesntExist(this.sourceService, this.targetService);
            }

            // If the setup specifies a data structure, use that. Otherwise get the structure from the source.
            var dataStructure = this.setup.DataStructure ?? this.sourceService.GetStructure();

            this.dataFactory.CreateDataFactory();

            this.dataFactory.LinkService(this.sourceService, this.setup.SourceLinkedServiceName);
            this.dataFactory.LinkService(this.targetService, this.setup.TargetLinkedServiceName);

            this.dataFactory.CreateDatasets(dataStructure);

            // TODO make these configurable
            DateTime pipelineActivePeriodStartTime = DateTime.Now.ToUniversalTime().AddHours(-100);
            DateTime pipelineActivePeriodEndTime = pipelineActivePeriodStartTime.AddMinutes(1);

            this.dataFactory.CreatePipeline(pipelineActivePeriodStartTime, pipelineActivePeriodEndTime);
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

        internal void DeleteDataFactoryIfExists()
        {
            if (this.dataFactory.DataFactoryExists())
            {
                this.dataFactory.Delete();
            }
        }
    }
}