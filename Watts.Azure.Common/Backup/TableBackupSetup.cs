namespace Watts.Azure.Common.Backup
{
    using System;
    using Interfaces.Storage;

    public class TableBackupSetup
    {
        /// <summary>
        /// The table to back up.
        /// </summary>
        public IAzureTableStorage SourceStorage { get; set; }

        /// <summary>
        /// The frequency with which to perform incremental backups to the current target.
        /// </summary>
        public TimeSpan IncrementalChangesFrequency { get; set; }

        /// <summary>
        /// The frequency with which to switch the table target.
        /// </summary>
        public TimeSpan SwitchTargetFrequency { get; set; }

        /// <summary>
        /// The time to keep the backup.
        /// </summary>
        public TimeSpan RetentionTime { get; set; }

        public BackupMode BackupMode { get; set; }

        public int TimeoutInMinutes { get; set; }
    }
}