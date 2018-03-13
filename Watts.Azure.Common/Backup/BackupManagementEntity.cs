namespace Watts.Azure.Common.Backup
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class BackupManagementEntity : TableEntity
    {
        public BackupManagementEntity()
        {
        }

        public BackupManagementEntity(Guid id, string sourceTableName, string sourceTableSubscriptionId)
        {
            this.Id = id;
            this.SourceTableName = sourceTableName;
            this.SourceTableSubscriptionId = sourceTableSubscriptionId;

            this.PartitionKey = this.Id.ToString();
            this.RowKey = this.SourceTableSubscriptionId;
        }

        public Guid Id { get; set; }

        public string SourceTableName { get; set; }

        public string SourceTableSubscriptionId { get; set; }

        public string TargetStorageAccountName { get; set; }

        public string TargetTableName { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset BackupStartedAt { get; set; }

        public DateTimeOffset BackupFinishedAt { get; set; }

        public BackupStatus Status { get; set; }
    }
}