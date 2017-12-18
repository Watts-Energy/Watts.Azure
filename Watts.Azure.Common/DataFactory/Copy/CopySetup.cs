namespace Watts.Azure.Common.DataFactory.Copy
{
    /// <summary>
    /// Setup for a data copy
    /// </summary>
    public class CopySetup
    {
        public string CopyPipelineName { get; set; }

        /// <summary>
        /// The name that should be given to the source data set (you pick one).
        /// </summary>
        public string SourceDatasetName { get; set; }

        /// <summary>
        /// The name that should be given to the target data set (you pick one).
        /// If copying to Data lake, this will be the name of the target text file.
        /// </summary>
        public string TargetDatasetName { get; set; }

        /// <summary>
        /// The name that should be given to the source linked service (you pick one).
        /// </summary>
        public string SourceLinkedServiceName { get; set; }

        /// <summary>
        /// The name that should be given to the target linked service (you pick one).
        /// </summary>
        public string TargetLinkedServiceName { get; set; }

        /// <summary>
        /// The time out in minutes of the copy operation.
        /// </summary>
        public int TimeoutInMinutes { get; set; }

        /// <summary>
        /// A boolean indicating whether the target data store should be created if it doesn't exist.
        /// What this means depends on the type of target service. If Table Storage the table is created, if DataLake,
        /// the directory that the data lake store points to is created.
        /// </summary>
        public bool CreateTargetIfNotExists { get; set; } = false;

        public bool DeleteDataFactoryIfExists { get; set; } = false;
    }
}