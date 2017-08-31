namespace Watts.Azure.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Batch;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common;
    using Watts.Azure.Common.Batch.Objects;
    using Watts.Azure.Common.General;
    using Watts.Azure.Utils;
    using Watts.Azure.Utils.Helpers.Batch;

    [TestClass]
    public class ConsoleCommandHelperUnitTests
    {
        /// <summary>
        /// Tests that constructing a single Windows command is done correctly, when the command has no additional arguments.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void CombinesSingleCommandWithInputFileNamesCorrectly()
        {
            // ARRANGE
            ConsoleCommandHelper testSubject = new ConsoleCommandHelper();
            ResourceFile resourceFile = new ResourceFile("blob", "filePath");

            string scriptName = "myScript.R";

            BatchConsoleCommand command = new BatchConsoleCommand()
            {
                BaseCommand = string.Format(CommandLibrary.AzureRScriptCommandWindows.Replace("@rVersion", Globals.DefaultRVersion).Replace("@rScriptName", scriptName))
            };

            // ACT
            var constructedCommand = testSubject.ConstructCommand(
                    new List<BatchConsoleCommand>() { command },
                    resourceFile,
                    null,
                    OperatingSystemFamily.Windows);

            // ASSERT
            string expectedResult = $"cmd /c {command.BaseCommand + " " + resourceFile.FilePath}";

            Assert.AreEqual(expectedResult, constructedCommand);
        }

        /// <summary>
        /// Tests that combining multiple Windows commands, none of which specify additional arguments, is done correctly.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void CombinesMultipleCommandsWithInputFileNamesCorrectly()
        {
            // ARRANGE
            ConsoleCommandHelper testSubject = new ConsoleCommandHelper();
            ResourceFile resourceFile = new ResourceFile("blob", "filePath");

            string scriptName = "myScript.R";
            string programFileName = "SomeExecutable.exe";

            BatchConsoleCommand command = new BatchConsoleCommand()
            {
                BaseCommand = string.Format(CommandLibrary.AzureRScriptCommandWindows.Replace("@rVersion", Globals.DefaultRVersion).Replace("@rScriptName", scriptName))
            };

            BatchConsoleCommand command2 = new BatchConsoleCommand()
            {
                BaseCommand = programFileName
            };

            // ACT
            var constructedCommand = testSubject.ConstructCommand(
                    new List<BatchConsoleCommand>() { command, command2 },
                    resourceFile,
                    null,
                    OperatingSystemFamily.Windows);

            // ASSERT
            string expectedResult = $"cmd /c {command.BaseCommand + " " + resourceFile.FilePath} && {programFileName} {resourceFile.FilePath}";

            Assert.AreEqual(expectedResult.Trim(), constructedCommand.Trim());
        }

        /// <summary>
        /// Tests that when multiple Windows commands are combined, which all have arguments, the command is as expected.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void CombinesMultipleCommands_WithArguments_WithInputFileNamesCorrectly()
        {
            // ARRANGE
            ConsoleCommandHelper testSubject = new ConsoleCommandHelper();
            ResourceFile resourceFile = new ResourceFile("blob", "filePath");

            string scriptName = "myScript.R";
            string programFileName = "SomeExecutable.exe";

            BatchConsoleCommand command = new BatchConsoleCommand()
            {
                BaseCommand = string.Format(CommandLibrary.AzureRScriptCommandWindows.Replace("@rVersion", Globals.DefaultRVersion).Replace("@rScriptName", scriptName)),
                Arguments = new List<string>() { "arg1" }
            };

            BatchConsoleCommand command2 = new BatchConsoleCommand()
            {
                BaseCommand = programFileName,
                Arguments = new List<string>() { "arg2" }
            };

            // ACT
            var constructedCommand = testSubject.ConstructCommand(
                        new List<BatchConsoleCommand>() { command, command2 },
                        resourceFile,
                        null,
                        OperatingSystemFamily.Windows);

            string expectedResult = $"cmd /c {command.BaseCommand + " " + resourceFile.FilePath + " " + command.Arguments.First()} && {programFileName} {resourceFile.FilePath} {command2.Arguments.First()}";

            // ASSERT
            Assert.AreEqual(expectedResult.Trim(), constructedCommand.Trim());
        }
    }
}