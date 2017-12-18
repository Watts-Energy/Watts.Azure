namespace Watts.Azure.Tests.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Azure.Utils;
    using Azure.Utils.Helpers.Batch;
    using Common;
    using Common.Batch.Objects;
    using Common.General;
    using FluentAssertions;
    using Microsoft.Azure.Batch;
    using NUnit.Framework;

    [TestFixture]
    public class ConsoleCommandHelperUnitTests
    {
        /// <summary>
        /// Tests that constructing a single Windows command is done correctly, when the command has no additional arguments.
        /// </summary>
        [Test]
        [Category("UnitTest")]
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
            constructedCommand
                .Should().StartWith("cmd /c")
                .And
                .EndWith(resourceFile.FilePath, "because it should execute on the given input file")
                .And.Contain(command.BaseCommand, "must specify the base command given");
        }

        /// <summary>
        /// Tests that combining multiple Windows commands, none of which specify additional arguments, is done correctly.
        /// </summary>
        [Test]
        [Category("UnitTest")]
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
            constructedCommand.Should()
                .StartWith("cmd /c")
                .And
                .Contain($"{command.BaseCommand} {resourceFile.FilePath} &&",
                    "because it should run the base command on the given input file")
                .And
                .Contain("&&", "because it combines multiple commands")
                .And
                .EndWith($"{programFileName} {resourceFile.FilePath}", "because it should run the second command last");
        }

        /// <summary>
        /// Tests that when multiple Windows commands are combined, which all have arguments, the command is as expected.
        /// </summary>
        [Test]
        [Category("UnitTest")]
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

            // ASSERT
            constructedCommand.Should()
                .StartWith("cmd /c", "because it was constructed for windows")
                .And
                .Contain($"{command.BaseCommand} {resourceFile.FilePath} {command.Arguments.First()}", "because it needs to run the base command on the input file with one argument")
                .And
                .Contain("&&", "because it combines multiple commands")
                .And
                .EndWith($"{programFileName} {resourceFile.FilePath} {command2.Arguments.First()}",
                    "because it should run the second program on the input file and add an argument");
        }
    }
}