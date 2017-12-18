namespace Watts.Azure.Common.DataFactory.Copy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using General;
    using Hyak.Common;
    using Interfaces.Security;
    using Microsoft.Azure;
    using Microsoft.Azure.Management.DataFactories;
    using Microsoft.Azure.Management.DataFactories.Common.Models;
    using Microsoft.Azure.Management.DataFactories.Models;
    using Storage.Objects;
    using Watts.Azure.Common.Interfaces.DataFactory;

    /// <summary>
    /// Azure Data Factory copy of data from one storage service to another
    /// </summary>
    public class AzureDataFactoryCopy
    {
        private readonly AzureDatasetHelper datasetHelper;
        private readonly LinkedServiceHelper linkedServiceHelper;
        private readonly AzureDataFactorySetup factorySetup;
        private readonly IAzureActiveDirectoryAuthentication authentication;
        private readonly CopySetup copySetup;
        private readonly Action<string> progressDelegate;

        private IAzureLinkedService sourceService;
        private IAzureLinkedService targetService;
        private CopySource source;
        private CopySink sink;
        private int authenticationRetries = 0;

        public AzureDataFactoryCopy(AzureDataFactorySetup factorySetup, CopySetup copySetup, IAzureActiveDirectoryAuthentication authentication, Action<string> progressDelegate = null)
        {
            this.datasetHelper = new AzureDatasetHelper();
            this.linkedServiceHelper = new LinkedServiceHelper(progressDelegate);
            this.factorySetup = factorySetup;
            this.copySetup = copySetup;
            this.authentication = authentication;
            this.progressDelegate = progressDelegate;

            Uri resourceManagerUri = new Uri(Constants.ResourceManagerEndpoint);

            this.Client = new DataFactoryManagementClient(authentication.GetTokenCredentials(), resourceManagerUri);
        }

        public IAzureLinkedService SourceService
        {
            get => this.sourceService;

            set
            {
                this.sourceService = value;
                this.source = this.datasetHelper.GetCopySource(this.sourceService);
            }
        }

        public IAzureLinkedService TargetService
        {
            get => this.targetService;

            set
            {
                this.targetService = value;
                this.sink = this.datasetHelper.GetCopySink(this.targetService);
            }
        }

        public DataFactoryManagementClient Client { get; private set; }

        /// <summary>
        /// Apply a filter query to the copy.
        /// </summary>
        /// <param name="queryString"></param>
        public void UsingSourceQuery(string queryString)
        {
            this.Report($"Will use source query {queryString}");
            this.datasetHelper.SetSourceQuery(this.source, queryString);
        }

        /// <summary>
        /// Link a service to the copy (e.g. the source or the sink)
        /// </summary>
        /// <param name="service"></param>
        /// <param name="linkedServiceName"></param>
        /// <returns></returns>
        public bool LinkService(IAzureLinkedService service, string linkedServiceName)
        {
            this.Report($"Adding linked service {linkedServiceName}");

            LinkedServiceTypeProperties serviceTypeProperties = this.linkedServiceHelper.GetLinkedServiceTypeProperties(service);

            var result = this.Client.LinkedServices.CreateOrUpdate(
                this.factorySetup.ResourceGroupName,
                this.factorySetup.Name,
                new LinkedServiceCreateOrUpdateParameters()
                {
                    LinkedService = new LinkedService()
                    {
                        Name = linkedServiceName,
                        Properties = new LinkedServiceProperties(serviceTypeProperties),
                    }
                });

            this.Report($"Result status {result.Status}");
            return result.Status == OperationStatus.Succeeded;
        }

        /// <summary>
        /// Create the datasets for the source and sink.
        /// </summary>
        /// <param name="dataStructure"></param>
        /// <returns></returns>
        public bool CreateDatasets(DataStructure dataStructure)
        {
            var structure = dataStructure.DataElements;

            this.Report($"Creating source dataset with structure {string.Join(", ", structure.Select(p => p.Name))}");
            var sourceResult = this.CreateSourceDataSet(structure);

            this.Report("Creating target dataset");
            var targetResult = this.CreateTargetDataSet(structure);

            this.Report($"Source dataset creation status {sourceResult.Status}");
            this.Report($"Target dataset creation status {targetResult.Status}");
            return sourceResult.Status == OperationStatus.Succeeded && targetResult.Status == OperationStatus.Succeeded;
        }

        /// <summary>
        /// Get the pipeline if it exists, otherwise null.
        /// </summary>
        /// <returns></returns>
        public Pipeline PipelineExists()
        {
            this.Report("Checking if pipeline exists...");
            PipelineListResponse pipeline = null;

            try
            {
                pipeline = this.Client.Pipelines.List(this.factorySetup.ResourceGroupName, this.factorySetup.Name);
            }
            catch (CloudException ex)
            {
                this.Report($"Exception when attempting to list pipelines. Assuming the pipeline does not exist... {ex}");
                return null;
            }

            if (pipeline.Pipelines.Count > 0)
            {
                this.Report("Pipeline exists, returning first");
                return pipeline.Pipelines.First();
            }

            this.Report("No pipeline found");
            return null;
        }

        public bool DataFactoryExists()
        {
            var dataFactory = this.Client.DataFactories.Get(this.factorySetup.ResourceGroupName, this.factorySetup.Name);

            return dataFactory.DataFactory != null;
        }

        /// <summary>
        /// Create the copy data pipeline.
        /// </summary>
        /// <param name="pipelineActivePeriodStartTime"></param>
        /// <param name="pipelineActivePeriodEndTime"></param>
        public void CreatePipeline(DateTime pipelineActivePeriodStartTime, DateTime pipelineActivePeriodEndTime)
        {
            this.Report($"Creating pipeline {this.copySetup.SourceDatasetName} -> {this.copySetup.TargetDatasetName}...");

            this.Client.Pipelines.CreateOrUpdate(
                this.factorySetup.ResourceGroupName,
                this.factorySetup.Name,
                new PipelineCreateOrUpdateParameters()
                {
                    Pipeline = new Pipeline()
                    {
                        Name = this.copySetup.CopyPipelineName ?? Guid.NewGuid().ToString(),
                        Properties = new PipelineProperties()
                        {
                            Description = "Pipeline for data transfer between two services",

                            // Initial value for pipeline's active period. With this, you won't need to set slice status
                            Start = pipelineActivePeriodStartTime,
                            End = pipelineActivePeriodEndTime,

                            Activities = new List<Activity>()
                        {
                            new Activity()
                            {
                                Name = Guid.NewGuid().ToString(),
                                Inputs = new List<ActivityInput>()
                                {
                                    new ActivityInput() { Name = this.copySetup.SourceDatasetName }
                                },
                                Outputs = new List<ActivityOutput>()
                                {
                                    new ActivityOutput()
                                    {
                                        Name = this.copySetup.TargetDatasetName
                                    }
                                },
                                TypeProperties = new CopyActivity()
                                {
                                    Source = this.source,
                                    Sink = this.sink
                                }
                            }
                        },
                        }
                    }
                });
        }

        /// <summary>
        /// Monitor the status of the data copy, exiting when the copy either times out or is completed.
        /// </summary>
        /// <param name="destinationDatasetName"></param>
        /// <param name="pipelineActivePeriodStartTime"></param>
        /// <param name="pipelineActivePeriodEndTime"></param>
        public void MonitorStatusUntilDone(string destinationDatasetName, DateTime pipelineActivePeriodStartTime, DateTime pipelineActivePeriodEndTime)
        {
            DateTime start = DateTime.Now;
            bool done = false;

            while (DateTime.Now - start < TimeSpan.FromMinutes(this.copySetup.TimeoutInMinutes) && !done)
            {
                try
                {
                    this.Report("Pulling the slice status");

                    // Wait before the next status check
                    Thread.Sleep(1000 * 12);

                    var datalistResponse = this.Client.DataSlices.List(
                        this.factorySetup.ResourceGroupName,
                        this.factorySetup.Name,
                        destinationDatasetName,
                        new DataSliceListParameters()
                        {
                            DataSliceRangeStartTime = pipelineActivePeriodStartTime.ConvertToISO8601DateTimeString(),
                            DataSliceRangeEndTime = pipelineActivePeriodEndTime.ConvertToISO8601DateTimeString()
                        });

                    foreach (DataSlice slice in datalistResponse.DataSlices)
                    {
                        if (slice.State == DataSliceState.Failed || slice.State == DataSliceState.Ready)
                        {
                            this.Report($"Slice execution is done with status: {slice.State}");
                            done = true;
                            break;
                        }
                        else
                        {
                            this.Report($"Slice status is: {slice.State}");
                        }
                    }

                    // Reset the counter so we know that we're authenticated...
                    this.authenticationRetries = 0;
                }
                catch (Exception)
                {
                    this.Report("Exception occurred...");
                    if (this.authenticationRetries < 3)
                    {
                        this.Report("Could be a temporary disconnect, will attempt to re-authenticate...");
                        this.RetryAuthentication();
                        this.authenticationRetries++;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of all error messages that occcured during the execution.
        /// </summary>
        /// <param name="pipelineActivePeriodStartTime"></param>
        /// <returns></returns>
        public List<string> GetErrors(DateTime pipelineActivePeriodStartTime)
        {
            List<string> retVal = new List<string>();

            var datasliceRunListResponse = this.Client.DataSliceRuns.List(
                    this.factorySetup.ResourceGroupName,
                    this.factorySetup.Name,
                    this.copySetup.TargetDatasetName,
                    new DataSliceRunListParameters()
                    {
                        DataSliceStartTime = pipelineActivePeriodStartTime.ConvertToISO8601DateTimeString()
                    });

            retVal = datasliceRunListResponse.DataSliceRuns.Where(p => !string.IsNullOrEmpty(p.ErrorMessage))
                .Select(q => q.ErrorMessage).ToList();

            return retVal;
        }

        /// <summary>
        /// Print all details of the run (status, start time, end time, etc.)
        /// </summary>
        /// <param name="destinationDatasetName"></param>
        /// <param name="pipelineActivePeriodStartTime"></param>
        public void PrintRunDetails(string destinationDatasetName, DateTime pipelineActivePeriodStartTime)
        {
            var datasliceRunListResponse = this.Client.DataSliceRuns.List(
                    this.factorySetup.ResourceGroupName,
                    this.factorySetup.Name,
                    destinationDatasetName,
                    new DataSliceRunListParameters()
                    {
                        DataSliceStartTime = pipelineActivePeriodStartTime.ConvertToISO8601DateTimeString()
                    });

            foreach (DataSliceRun run in datasliceRunListResponse.DataSliceRuns)
            {
                this.Report($"Status: \t\t{run.Status}");
                this.Report($"DataSliceStart: \t{run.DataSliceStart}");
                this.Report($"DataSliceEnd: \t\t{run.DataSliceEnd}");
                this.Report($"ActivityId: \t\t{run.ActivityName}");
                this.Report($"ProcessingStartTime: \t{run.ProcessingStartTime}");
                this.Report($"ProcessingEndTime: \t{run.ProcessingEndTime}");
                this.Report($"ErrorMessage: \t{run.ErrorMessage}");
            }
        }

        /// <summary>
        /// Delete the datafactory associated with the copy.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            var response = this.Client.DataFactories.Delete(this.factorySetup.ResourceGroupName, this.factorySetup.Name);

            // If the status code is 200 or 204, the delete was successful (https://docs.microsoft.com/en-us/rest/api/datafactory/integrationruntimes/delete)
            return response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Create the data factory
        /// </summary>
        internal void CreateDataFactory()
        {
            this.Report($"Creating datafactory {this.factorySetup.Name}...");
            this.Client.DataFactories.CreateOrUpdate(
                this.factorySetup.ResourceGroupName,
                new DataFactoryCreateOrUpdateParameters()
                {
                    DataFactory = new DataFactory()
                    {
                        Name = this.factorySetup.Name,
                        Location = "northeurope",
                        Properties = new DataFactoryProperties() { }
                    }
                });
        }

        /// <summary>
        /// Attempt to re-authenticate.
        /// </summary>
        internal void RetryAuthentication()
        {
            Console.WriteLine("Will re-authenticate with Azure");
            Uri resourceManagerUri = new Uri(Constants.ResourceManagerEndpoint);
            this.Client = new DataFactoryManagementClient(this.authentication.GetTokenCredentials(), resourceManagerUri);
        }

        /// <summary>
        /// Report progress on the progress delegate if there is one.
        /// </summary>
        /// <param name="progress"></param>
        internal void Report(string progress)
        {
            this.progressDelegate?.Invoke(progress);
        }

        internal DatasetCreateOrUpdateResponse CreateTargetDataSet(List<DataElement> structure)
        {
            return this.Client.Datasets.CreateOrUpdate(
                this.factorySetup.ResourceGroupName,
                this.factorySetup.Name,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = this.copySetup.TargetDatasetName,
                        Properties = new DatasetProperties()
                        {
                            LinkedServiceName = this.copySetup.TargetLinkedServiceName,
                            TypeProperties = this.datasetHelper.GetTypeProperties(this.TargetService, this.copySetup.TargetDatasetName),

                            Availability = new Availability()
                            {
                                Frequency = SchedulePeriod.Day,
                                Interval = 1,
                            },
                            Structure = structure
                        },
                    }
                });
        }

        internal DatasetCreateOrUpdateResponse CreateSourceDataSet(List<DataElement> structure)
        {
            return this.Client.Datasets.CreateOrUpdate(
                this.factorySetup.ResourceGroupName,
                this.factorySetup.Name,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = this.copySetup.SourceDatasetName,
                        Properties = new DatasetProperties()
                        {
                            LinkedServiceName = this.copySetup.SourceLinkedServiceName,
                            TypeProperties = this.datasetHelper.GetTypeProperties(this.SourceService),
                            External = true,
                            Availability = new Availability()
                            {
                                Frequency = SchedulePeriod.Day,
                                Interval = 1,
                            },
                            Policy = new Policy()
                            {
                                Validation = new ValidationPolicy()
                                {
                                    MinimumRows = 1
                                }
                            },
                            Structure = structure
                        }
                    }
                });
        }
    }
}