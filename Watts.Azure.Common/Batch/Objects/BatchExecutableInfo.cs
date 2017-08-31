namespace Watts.Azure.Common.Batch.Objects
{
    public class BatchExecutableInfo
    {
        public BatchExecutableInfo()
        {
            this.BatchExecutableContainerName = string.Empty;
            this.BatchInputContainerName = string.Empty;
        }

        public string BatchExecutableContainerName { get; set; }

        public string BatchInputContainerName { get; set; }
    }
}