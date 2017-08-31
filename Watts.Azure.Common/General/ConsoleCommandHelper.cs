namespace Watts.Azure.Common.General
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Batch.Objects;
    using Microsoft.Azure.Batch;
    using Constants = Common.Constants;

    /// <summary>
    /// Helper class for constructing console commands depending on Operating System.
    /// </summary>
    public class ConsoleCommandHelper
    {
        public string CombineConsoleCommands(List<BatchConsoleCommand> consoleCommands, ResourceFile inputFile)
        {
            return string.Join(
                        " && ",
                        consoleCommands.Select(
                            t =>
                                $"{t.BaseCommand} {inputFile.FilePath}{this.EmptyStringIfEmptyWhitespaceOtherwise(t.Arguments)}{string.Join(" ", t.Arguments)}"));
        }

        public string WrapConsoleCommand(string command, string outfile, OperatingSystemFamily operatingSystemFamily)
        {
            string batchTaskDirEnvironmentVariable = operatingSystemFamily == OperatingSystemFamily.Linux
                ? Constants.BatchTaskDirLinux
                : Constants.BatchTaskDirWindows;

            string redirectOutput = string.IsNullOrEmpty(outfile) ? string.Empty : $" > {batchTaskDirEnvironmentVariable}\\{outfile} 2>&1";

            switch (operatingSystemFamily)
            {
                case OperatingSystemFamily.Linux:
                    return $"/bin/bash -c \'set -e; set -o pipefail; {command}; wait{redirectOutput}\'";
                case OperatingSystemFamily.Windows:
                    return $"cmd /c {command}{redirectOutput}";

                default:
                    return string.Empty;
            }
        }

        public string ConstructCommand(List<BatchConsoleCommand> individualCommands, ResourceFile inputFile, string outfileName, OperatingSystemFamily operatingSystemFamily)
        {
            return this.WrapConsoleCommand(this.CombineConsoleCommands(individualCommands, inputFile), outfileName, operatingSystemFamily);
        }

        internal string EmptyStringIfEmptyWhitespaceOtherwise(IList list)
        {
            return list.Count == 0 ? string.Empty : " ";
        }
    }
}