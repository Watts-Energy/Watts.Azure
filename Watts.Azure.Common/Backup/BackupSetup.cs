namespace Watts.Azure.Common.Backup
{
    using System;
    using System.Collections.Generic;
    using DataFactory.General;
    using Interfaces.Storage;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Security;

    public class BackupSetup
    {
        /// <summary>
        /// The prefix of the backed up table. The name will be this prefix + the date when the backup target was created.
        /// </summary>
        public string BackupStorageAccountSuffix { get; set; }

        /// <summary>
        /// The name of the resource group to place backups in.
        /// </summary>
        public string BackupTargetResourceGroupName { get; set; }

        public AzureDataFactorySetup DataFactorySetup { get; set; }

        /// <summary>
        /// The region to place the backup storage account in.
        /// </summary>
        public Region BackupTargetRegion { get; set; }

        /// <summary>
        /// The tables that should be backed up to the target storage account.
        /// </summary>
        public IList<TableBackupSetup> TablesToBackup { get; set; }

        public AzureEnvironment AzureEnvironment { get; set; } = AzureEnvironment.AzureGlobalCloud;
    }
}