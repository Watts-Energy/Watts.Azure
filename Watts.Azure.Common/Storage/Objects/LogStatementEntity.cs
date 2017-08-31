namespace Watts.Azure.Common.Storage.Objects
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// A table entity in a log table, specifying the log level, application and statement
    /// </summary>
    public class LogStatementEntity : TableEntity
    {
        public LogStatementEntity()
        {
        }

        public LogStatementEntity(string statement, string application, string instanceId, LogLevel logLevel)
        {
            this.PartitionKey = application;
            this.RowKey = instanceId;
            this.Statement = statement;
            this.Application = application;
            this.ApplicationInstanceId = instanceId;
            this.LogLevel = logLevel.ToString();
        }

        public string LogLevel { get; set; }

        public string Statement { get; set; }

        public string Application { get; set; }

        public string ApplicationInstanceId { get; set; }
    }
}