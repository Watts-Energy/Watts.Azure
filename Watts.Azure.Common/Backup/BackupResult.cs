namespace Watts.Azure.Common.Backup
{
    public class BackupResult
    {
        public TableBackupSetup Setup { get; set; }

        public BackupReturnCode ReturnCode { get; set; }

        public string BackUpResourceGroup { get; set; }

        public string BackUpStorageAccountName { get; set; }

        public string BackUpTableName { get; set; }
    }
}