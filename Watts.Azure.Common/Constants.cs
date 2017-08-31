namespace Watts.Azure.Common
{
    using Batch.Objects;

    public class Constants
    {
        public static AzureMachineConfig SmallOneCore => new AzureMachineConfig()
        {
            Size = "small"
        };

        public static AzureMachineConfig MediumTwoCores => new AzureMachineConfig()
        {
            Size = "medium"
        };

        public static AzureMachineConfig LargeFourCores => new AzureMachineConfig()
        {
            Size = "large"
        };

        public static AzureMachineConfig ExtraLargeEightCores => new AzureMachineConfig()
        {
            Size = "extralarge"
        };

        public static AzureMachineConfig Standard_D1 => new AzureMachineConfig()
        {
            Size = "Standard_D1"
        };

        public static AzureMachineConfig Standard_D1_v2 => new AzureMachineConfig()
        {
            Size = "Standard_D1_v2"
        };

        public static AzureMachineConfig Standard_A1_v2 => new AzureMachineConfig()
        {
            Size = "Standard_A1_v2"
        };

        public static string ResourceManagerEndpoint => "https://management.azure.com/";

        public static string WindowsManagementUri => "https://management.core.windows.net/";

        public static string ActiveDirectoryEndpoint => "https://login.windows.net";

        public static string BatchTaskDirWindows => "%AZ_BATCH_TASK_DIR%";

        public static string BatchTaskDirLinux => "$AZ_BATCH_TASK_DIR";
    }
}