namespace Watts.Azure.Common.DataFactory.Copy
{
    /// <summary>
    /// Setup for a azure table -> azure table copy of data.
    /// </summary>
    public class CopyTableSetup
    {
        /// <summary>
        /// The name that should be given to the source data set (you pick one).
        /// </summary>
        public string SourceDatasetName { get; set; }

        /// <summary>
        /// The name that should be given to the target data set (you pick one).
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
        /// A boolean indicating whether the target table should be created if it doesn't exist.
        /// </summary>
        public bool CreateTargetTableIfNotExists { get; set; } = false;
    }
}