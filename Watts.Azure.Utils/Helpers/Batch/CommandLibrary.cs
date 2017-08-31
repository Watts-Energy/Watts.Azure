namespace Watts.Azure.Utils.Helpers.Batch
{
    /// <summary>
    /// Library of commands that are used in batch.
    /// </summary>
    public class CommandLibrary
    {
        public static string AzureRScriptCommandWindows => "%AZ_BATCH_APP_PACKAGE_R%\\R-@rVersion\\bin\\RScript.exe --max-mem-size=4000M %AZ_BATCH_NODE_SHARED_DIR%\\@rScriptName C:\\user\\tasks\\shared";

        public static string AzureRScriptCommandLinux
            =>
                "cp -r $AZ_BATCH_NODE_SHARED_DIR/* $AZ_BATCH_TASK_WORKING_DIR && Rscript $AZ_BATCH_TASK_WORKING_DIR/@rScriptName $AZ_BATCH_TASK_WORKING_DIR";

        public static string LinuxNodeStartupCommandInstallR
            =>
                "cp -r $AZ_BATCH_NODE_STARTUP_DIR/wd/* $AZ_BATCH_NODE_SHARED_DIR && printenv && apt-get update && apt-get install -y r-base";

        public static string InstallMonoOnUbuntu1404
            =>
                "sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && echo \"deb http://download.mono-project.com/repo/ubuntu trusty main\" | sudo tee /etc/apt/sources.list.d/mono-official.list && sudo apt-get update && sudo apt-get install -y mono-devel && sudo apt-get install -y mono-complete";

        public static string InstallMonoOnDebian8
            =>
                "sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && echo \"deb http://download.mono-project.com/repo/debian jessie main\" | sudo tee /etc/apt/sources.list.d/mono-official.list && sudo apt-get update && sudo apt-get install -y mono-devel && sudo apt-get install -y mono-complete";
    }
}