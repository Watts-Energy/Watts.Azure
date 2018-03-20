namespace Watts.Azure.Common.Backup
{
    using Interfaces.Storage;

    public class TableBackupSetup
    {
        /// <summary>
        /// The table to back up.
        /// </summary>
        public IAzureTableStorage SourceStorage { get; set; }

        public BackupSchedule Schedule { get; set; }

        public BackupMode BackupMode { get; set; }

        public int TimeoutInMinutes { get; set; }
    }
}