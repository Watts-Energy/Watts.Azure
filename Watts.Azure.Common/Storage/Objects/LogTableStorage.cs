namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces.General;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using LogLevel = Watts.Azure.Common.LogLevel;

    /// <summary>
    /// Table storage for a log.
    /// </summary>
    public class LogTableStorage : AzureTableStorage, ILog
    {
        private readonly string instanceId;
        private readonly string applicationName;

        public LogTableStorage(CloudStorageAccount account, string applicationName, string tableName = "Log") : base(account, tableName)
        {
            this.TableName = tableName;
            this.applicationName = applicationName;
            this.instanceId = Guid.NewGuid().ToString();
        }

        public string DisplayName => $"{this.TableName} {this.StorageAccount.TableEndpoint.AbsoluteUri}";

        public void Debug(string statement)
        {
            this.LogStatement(statement, LogLevel.Debug);
        }

        public void Info(string statement)
        {
            this.LogStatement(statement, LogLevel.Info);
        }

        public void Error(string statement)
        {
            this.LogStatement(statement, LogLevel.Error);
        }

        public void Fatal(string statement)
        {
            this.LogStatement(statement, LogLevel.Fatal);
        }

        public List<LogStatementEntity> GetLastDays(int count)
        {
            CloudTable table = this.TableClient.GetTableReference(this.TableName);

            DateTimeOffset startTime = DateTimeOffset.Now.Date.AddDays(-count);

            TableQuery<LogStatementEntity> query =
                new TableQuery<LogStatementEntity>().Where(
                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, startTime));

            var retVal = table.ExecuteQuery(query);
            return retVal.ToList();
        }

        internal void LogStatement(string statement, LogLevel level)
        {
            try
            {
                var logEntity = new LogStatementEntity(statement, this.applicationName, this.instanceId, level);

                this.Insert(logEntity);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An exception occurred when attempting to log the statement {0} at level {1}. Exception: {2}", statement, level, ex);
            }
        }
    }
}