namespace Watts.Azure.Common.Backup
{
    using System;

    public class BackupSchedule
    {
        /// <summary>
        /// The frequency with which to switch the table target.
        /// </summary>
        public TimeSpan SwitchTargetStorageFrequency { get; set; }

        /// <summary>
        /// The frequency with which to perform incremental backups to the current target.
        /// </summary>
        public TimeSpan IncrementalLoadFrequency { get; set; }

        /// <summary>
        /// The time to keep the backup.
        /// </summary>
        public TimeSpan RetentionTimeSpan { get; set; }
    }
}