namespace Watts.Azure.Common.Batch.Objects
{
    public class BatchOutputContainer
    {
        /// <summary>
        /// Container for the output of a batch (i.e. the standard out and error)
        /// </summary>
        /// <param name="connectionString"></param>
        public BatchOutputContainer(string connectionString)
        {
            this.ConnectionString = connectionString;
            this.Name = "batchoutput";
        }

        public string ConnectionString { get; set; }

        public string Name { get; }
    }
}