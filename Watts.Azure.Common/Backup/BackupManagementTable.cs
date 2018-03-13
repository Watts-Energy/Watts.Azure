namespace Watts.Azure.Common.Backup
{
    using Storage.Objects;

    public class BackupManagementTable : AzureTableStorage
    {
        public BackupManagementTable(string connectionString) : base("BackupManagement", connectionString)
        {
        }
    }
}