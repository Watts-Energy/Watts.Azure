namespace Watts.Azure.Common.Batch
{
    using System.Collections.Generic;
    using Objects;

    /// <summary>
    /// A command line execution in batch. This class contains helper methods that make the construction of these easier.
    /// </summary>
    public class BatchCommand
    {
        /// <summary>
        /// A predefined command for running an executable on a node in Azure batch.
        /// Note that it runs the command from the shared node directory with cmd /c.
        /// An argument to pass to the executable can optionally be specified.
        /// </summary>
        /// <param name="executableName"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static BatchConsoleCommand GetRunExecutableOnInputFileWithArgumentComand(string executableName, string argument)
        {
            return new BatchConsoleCommand()
            {
                BaseCommand = $"cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\{executableName}",
                Arguments = new List<string>() { argument }
            };
        }

        /// <summary>
        /// A predefined console command that copies all files served as input to a batch execution into the shared directory on nodes.
        /// </summary>
        /// <returns></returns>
        public static BatchConsoleCommand GetCopyTaskApplicationFilesToNodeSharedDirectory()
        {
            return new BatchConsoleCommand()
            {
                BaseCommand =
                    "cmd /c (robocopy %AZ_BATCH_TASK_WORKING_DIR% %AZ_BATCH_NODE_SHARED_DIR%) ^& IF %ERRORLEVEL% LEQ 1 exit 0",
                Arguments = new List<string>() { }
            };
        }
    }
}