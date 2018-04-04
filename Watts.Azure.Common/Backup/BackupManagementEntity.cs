namespace Watts.Azure.Common.Backup
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class BackupManagementEntity : TableEntity
    {
        public BackupManagementEntity()
        {
        }

        public BackupManagementEntity(string id, string sourceTableName, string targetStorageAccountName, string targetTableName, DateTimeOffset dateCreated, DateTimeOffset backupStartedAt, DateTimeOffset? backupFinishedAt, BackupStatus status, BackupMode backupMode)
        {
            this.Id = id;
            this.SourceTableName = sourceTableName;
            this.TargetStorageAccountName = targetStorageAccountName;
            this.TargetTableName = targetTableName;
            this.DateCreated = dateCreated;
            this.BackupStartedAt = backupStartedAt;
            this.BackupFinishedAt = backupFinishedAt;
            this.Status = (int)status;
            this.BackupMode = (int)backupMode;

            this.PartitionKey = this.Id;
            this.RowKey = dateCreated.ToString("yyyy-MM-dd");
        }

        public string Id { get; set; }

        public string SourceTableName { get; set; }

        public string TargetStorageAccountName { get; set; }

        public string TargetTableName { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset BackupStartedAt { get; set; }

        public DateTimeOffset? BackupFinishedAt { get; set; }

        public int Status { get; set; }

        public int BackupMode { get; set; }
    }
}