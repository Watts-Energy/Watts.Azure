namespace Watts.Azure.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Watts.Azure.Common;
    using Watts.Azure.Common.Batch.Objects;
    using Watts.Azure.Common.General;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Common.Storage.Objects.Wrappers;
    using Watts.Azure.Tests.Objects;
    using Watts.Azure.Utils.Build;
    using Watts.Azure.Utils.Helpers.Batch;
    using Watts.Azure.Utils.Interfaces.Batch;

    [TestClass]
    public class ExecuteBatchManualTests
    {
        private IBatchEnvironment environment;

        /// <summary>
        /// Setup the test by creating the environment.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Set your environment here (i.e. create a class implementing IBatchEnvironment and instantiate it here).
            // Or simply fill the properties
            this.environment = new TestEnvironmentConfigHandler(Constants.CredentialsFilePath).GetTestEnvironment().BatchEnvironment;
        }

        /// <summary>
        /// Tests that is is possible to find a linux image in the azure marketplace, by its id.
        /// </summary>
        [TestMethod]
        [TestCategory("ManualTest")]
        public void GetLinuxBoxById_FindsImage()
        {
            // ARRANGE
            var credentials = BatchBuilder.InEnvironment(this.environment).Credentials;

            AzureBatchClient client = new AzureBatchClient(credentials);

            // ACT
            var image = PredefinedMachines.GetLinuxConfigurationById(client, "batch.node.debian 8");

            // ASSERT
            Assert.IsNotNull(image);
        }

        /// <summary>
        /// Runs a simple R script on linux.
        /// Note that this does not test correctness at all, simply that the execution works (does not throw exceptions).
        /// This is meant mainly as a documentation test.
        /// </summary>
        [TestMethod]
        [TestCategory("ManualTest")]
        public void RunSimpleRScriptOnLinux()
        {
            // ARRANGE
            // Define some R code to execute.
            var codeToExecute = new string[]
            {
                "test <- read.csv2(\"inputFile1.txt\")",
                "print(test)"
            };

            var builder = BatchBuilder
                .InEnvironment(this.environment)
                .ResolveDependenciesUsing(DependencyResolver.UsingFunction(() => new string[] { }))
                .WithPoolSetup(new BatchPoolSetup()
                {
                    PoolId = "ExecuteBatchIntegrationTests",
                    JobId = "RunSimpleRScriptOnLinux"
                });

            // Get a machine configuration specifying one ubuntu instance of type 'small'
            var machineConfiguration =
                AzureMachineConfig.Small()
                    .Ubuntu(new AzureBatchClient(builder.Credentials))
                    .Instances(1);

            var inputPreparationDelegate = PrepareInputFiles.UsingFunction(() =>
            {
                string inputFile = "inputFile1.txt";
                File.WriteAllLines(inputFile, new string[] { "inputtest" });

                return new List<string>() { inputFile };
            });

            // ACT
            builder.ConfigureMachines(machineConfiguration)
                .PrepareInputUsing(inputPreparationDelegate)
                .DontSaveStatistics()
                .ExecuteRCode(codeToExecute)
                .GetBatchExecution()
                .StartBatch()
                .Wait();
        }

        /// <summary>
        /// Tests that running a simple R script on windows works (does not throw exceptions).
        /// Note that this does not assert correctness, it merely checks that the execution can be done without any exceptions being
        /// thrown by the code.
        /// </summary>
        [TestMethod]
        [TestCategory("ManualTest")]
        public void RunSimpleRScriptOWindows()
        {
            // ARRANGE
            var builder = BatchBuilder
                .InEnvironment(this.environment)
                .ResolveDependenciesUsing(DependencyResolver.UsingFunction(() => new string[] { }))
                .WithPoolSetup(new BatchPoolSetup()
                {
                    PoolId = "ExecuteBatchIntegrationTestsWindows",
                    JobId = "RunSimpleRScriptOnWindows"
                });

            // ACT
            builder.ConfigureMachines(AzureMachineConfig.Small().WindowsServer2012R2().Instances(1))
                .PrepareInputUsing(
                PrepareInputFiles.UsingFunction(() =>
                {
                    string inputFile = "inputFile1.txt";
                    File.WriteAllLines(inputFile, new string[] { "inputtest" });

                    return new List<string>() { inputFile };
                }))
                .DontSaveStatistics()
                .ExecuteRCode(new string[]
                {
                    "test <- read.csv2(\"inputFile1.txt\")",
                    "print(test)"
                })
                .GetBatchExecution()
                .StartBatch()
                .Wait();
        }

        /// <summary>
        /// Tests that running a simple R script on windows does not throw exceptions and that output can be saved and downloaded.
        /// Note that this does not test correctness, it merely checks that the execution can be done without any exceptions being
        /// thrown by the code.
        /// </summary>
        [TestMethod]
        [TestCategory("ManualTest")]
        public void RunSimpleRScriptOWindowsAndStoreOutput()
        {
            // ARRANGE
            BatchOutputContainer outputContainer = new BatchOutputContainer(this.environment.GetBatchStorageConnectionString());
            AzureBlobStorage outputStorage = AzureBlobStorage.Connect(this.environment.GetBatchStorageConnectionString(), outputContainer.Name);
            string relativePathToOutputHelper =
                "..\\..\\..\\Watts.Azure.Common.OutputHelper\\bin\\Debug\\Watts.Azure.Common.OutputHelper.exe";

            // Delete the output container if it already exists (it will be re-created).
            outputStorage.DeleteContainerIfExists();

            var builder = BatchBuilder
                .InEnvironment(this.environment)
                .ResolveDependenciesUsing(new NetFrameworkDependencies(relativePathToOutputHelper))
                .WithPoolSetup(new BatchPoolSetup()
                {
                    PoolId = "ExecuteWindowsStoreOutput",
                    JobId = "RunSimpleRScriptOnWindowsWithOutput"
                });

            var batchExecution = builder.ConfigureMachines(AzureMachineConfig.Small().WindowsServer2012R2().Instances(1))
                .PrepareInputUsing(
                    PrepareInputFiles.UsingFunction(() =>
                    {
                        string inputFile = "inputFile1.txt";
                        File.WriteAllLines(inputFile, new string[] { "inputtest" });

                        return new List<string>() { inputFile };
                    }))
                .ReportingProgressUsing(p => Trace.WriteLine(p))
                .DontSaveStatistics()
                .UploadOutputTo(outputContainer)
                .ExecuteRCode(new string[]
                {
                    "test <- read.csv2(\"inputFile1.txt\")",
                    "print(test)"
                })
                .GetBatchExecution();

            // ACT
            batchExecution.StartBatch().Wait();

            // ASSERT
            // Verify that there's output
            var output = batchExecution.GetExecutionOutput().First().Output;

            Assert.AreNotEqual(0, output.Count);
        }

        /// <summary>
        /// Tests that it is possible to execute a 'Hybrid' batch, i.e. one where two tasks are executed in order on each machine.
        /// The first task is to execute an R script that writes Hello World to a file, and the next task then reads the file and writes it to stdout.
        /// The test verifies that the output from the task (which is uploaded to blob by OutputHelper.exe) actually contains the strings
        /// 'Hello' and 'World'.
        /// </summary>
        [TestCategory("ManualTest")]
        [TestMethod]
        public void ExecuteHybridBatch()
        {
            // ARRANGE
            // Prepare the output container that will hold the batch output
            BatchOutputContainer outputContainer = new BatchOutputContainer(this.environment.GetBatchStorageConnectionString());
            AzureBlobStorage outputStorage = AzureBlobStorage.Connect(this.environment.GetBatchStorageConnectionString(), outputContainer.Name);

            // Set the relative path to the outputhelper executable (which is responsible for uploading the task
            // outputs to a blob
            string relativePathToOutputHelper =
                "..\\..\\..\\Watts.Azure.Common.OutputHelper\\bin\\Debug\\Watts.Azure.Common.OutputHelper.exe";

            // Delete the output storage container if it exists
            outputStorage.DeleteContainerIfExists();

            // Create the builder with a specific job and pool id.
            var builder = BatchBuilder
                .InEnvironment(this.environment)
                .ResolveDependenciesUsing(new NetFrameworkDependencies(relativePathToOutputHelper))
                .WithPoolSetup(new BatchPoolSetup() { JobId = "HybridBatchTestJob", PoolId = "HybridBatchTestPool" });

            // Prepare the first batch execution, which executes a piece of R code that saves
            // a string of text to  a file
            var machineConfig = AzureMachineConfig.Small()
                .WindowsServer2012R2()
                .Instances(1);

            var batchExecution = builder.ConfigureMachines(machineConfig)
            .PrepareInputUsing(
                PrepareInputFiles.UsingFunction(() =>
                {
                    string inputFile = "inputFile1.txt";
                    File.WriteAllLines(inputFile, new string[] { "This is the input" });

                    return new List<string>() { inputFile };
                }))
            .DontSaveStatistics()
            .UploadOutputTo(outputContainer)
            .ExecuteRCode(new string[]
            {
                    "test <- read.csv2(\"inputFile1.txt\")",
                    "fileConn<-file(\"output.txt\")",
                    "writeLines(c(\"Hello\",\"World\"), fileConn)",
                    "close(fileConn)"
            });

            // Prepare the second batch, which reads the file the first batch saved and writes the contents to stdout
            var secondBatch = BatchBuilder
                .AsNonPrimaryBatch(machineConfig)
                .UploadOutputTo(outputContainer)
                .ExecuteRCode(new string[]
                {
                    "theFileCreatedByTheLastScript <- read.table(\"./output.txt\")",
                    "theFileCreatedByTheLastScript"
                });

            // Create a hybrid batc that first executes the first batch followed by the second.
            var hybridExecution = HybridBatchExecution
                .First(batchExecution)
                .Then(secondBatch)
                .GetCombinedBatchExecutionBase();

            // ACT
            // Start it and wait for it to finish.
            hybridExecution
                .StartBatch()
                .Wait();

            // Verify that there's some output saved for the execution.
            var hybridBatchOutput = hybridExecution.GetExecutionOutput();

            // ASSERT
            // Assert that there is output, and that the strings Hello and World appear in the second and third output strings, respectively.
            Assert.AreNotEqual(0, hybridBatchOutput.Count);
            Assert.IsTrue(hybridBatchOutput[0].Output[1].Contains("Hello"));
            Assert.IsTrue(hybridBatchOutput[0].Output[2].Contains("World"));
        }
    }
}