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

        public string WrapConsoleCommand(string command, OperatingSystemFamily operatingSystemFamily)
        {
            string batchTaskDirEnvironmentVariable = operatingSystemFamily == OperatingSystemFamily.Linux
                ? Constants.BatchTaskDirLinux
                : Constants.BatchTaskDirWindows;

            switch (operatingSystemFamily)
            {
                case OperatingSystemFamily.Linux:
                    return $"/bin/bash -c \'set -e; set -o pipefail; {command}; wait\'";
                case OperatingSystemFamily.Windows:
                    return $"cmd /c {command}";

                default:
                    return string.Empty;
            }
        }

        public string ConstructCommand(List<BatchConsoleCommand> individualCommands, ResourceFile inputFile, OperatingSystemFamily operatingSystemFamily)
        {
            return this.WrapConsoleCommand(this.CombineConsoleCommands(individualCommands, inputFile), operatingSystemFamily);
        }

        internal string EmptyStringIfEmptyWhitespaceOtherwise(IList list)
        {
            return list.Count == 0 ? string.Empty : " ";
        }
    }
}