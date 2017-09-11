# Introduction

**Watts.Azure** provides utilities to e.g. run parallel computations implemented in e.g. *R*, *C#* or *python* in [Azure Batch](https://azure.microsoft.com/en-us/services/batch/) without 
having to know all the details and coding everything yourself as well as utilities to make working with [Azure Data Factory](https://azure.microsoft.com/en-us/services/data-factory/), [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/), 
[Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) and [Azure File Storage](https://azure.microsoft.com/en-us/services/storage/files/) easier.

**Watts.Azure** provides, among other things, a fluid interface that makes it simple to work with Azure Batch from .NET rather than having to 
code things from scratch and using the Azure Batch .NET API. It's by no means a complete suite of tools to work with Azure, but it's a starting point. Any contributions that make it more complete are extremely welcome!

It also makes it easy to set up backup in Azure Table Storage, by using the fluent interface for the [Azure Data Factory Copy Data activity](https://docs.microsoft.com/en-us/azure/data-factory/data-factory-data-movement-activities).

**Watts.Azure** has a lot of utilities, but is not everything we want it to be. Yet!
Be sure to check out the Issues section and feel free to add feature suggestions.

The reason that **Watts.Azure** exists is, that we found that working with Azure Batch from .NET (especially to execute R code) seemed needlessly cumbersome and required you to write a lot of boilerplate code.
We wanted create a simple interface to Azure Batch for those of us who do not need to know the details but would like to take advantage of the massive potential for scaling compute that it offers.

It also contains utilities that make working with services like Azure Table Storage, Azure Blob Storage and Azure Data Factory easier from .NET.

**NOTE**: The current compute nodes in Azure support .NET framework up to 4.5.2. There's currently no way to install higher versions without
requiring a restart of the node. If you're having trouble with executing your .NET executable in Batch, make sure you're targeting a version
<= 4.5.2 of the .NET framework.

Read on for some examples of how to use **Watts.Azure**.

# Azure Batch
Some key concepts in Azure Batch:
 * **Pool**: A number of provisioned virtual machines. These can be any of the types that Azure offers (https://docs.microsoft.com/en-us/azure/virtual-machines/windows/sizes)
 * **Task**: A command line execution of e.g. an executable or script that you uploaded to Azure.
 * **Job**: A collection of tasks.
 * **Node**: A member machine in the pool.

The overall idea used in batch is the following:
* Upload some executables (and dependencies) that you need to run on a lot of input files, to a blob (application blob).
* Upload all input files to another blob (input blob).
* Process the pool of tasks (i.e. run something on an input file), one on each node until completed. Each node receives a job (a command to execute, any executables you've added and one input file)
* Place all output files in an output blob (output blob).

As nodes are added to the pool in batch, a *start task* is executed on them. This can be pretty much anything, i.e. run a command, install an application.
It's just a console command.

When nodes are ready, they a take a task each and start processing it. Each task is the processing of one input file.
The input file is copied to the node and a command is run on the node, which executes a script or executable on that input file.
When it is done processing the task, it grabs a new one that hasn't been taken yet. 
This continues until all tasks have been processed.

Let's see **Watts.Azure** in action!
The following will execute an R-script named *myScript.R* on a single Windows Server R2 box in Azure Batch. In real life scenarios, you'd obviously want to perform this on possibly hundreds of nodes simultaneously.

**IMPORTANT NOTE:** If you want to run R in Azure Batch on VMs running Windows, a tiny amount of legwork is neccesary: See the section [**NOTE ON RUNNING R IN AZURE BATCH**](#note-on-running-r-in-azure-batch):

```cs
 var batch = BatchBuilder
    .InPredefinedEnvironment(environment)
    .ResolveDependenciesUsing(new ResolveRScriptDependencies("./"))
    .WithDefaultPoolSetup()
    .ConfigureMachines(AzureMachineConfig.StandardD1_V2().Instances(numberOfNodes))
    .PrepareInputUsing(filePreparation)
    .ReportProgressToConsole()
    .SetTimeoutInMinutes(1600)
    .ExecuteRScript("./myScript.R")
    .WithAdditionalScriptExecutionArgument(argument1)
    .WithAdditionalScriptExecutionArgument(argument2)
    .GetBatchExecution()
    .StartBatch();

 batch.Wait();
```

Here, **environment** is a class implementing *IPredefinedBatchEnvironment*, specifying the credentials for the batch account to use, and the
credentials of the storage account where it's to put the application, input and (possibly) output files.
You can create your own or simply use:

```cs
 IPredefinedBatchEnvironment env = new PredefinedBatchEnvironment()
 {
     BatchAccountSettings = new BatchAccountSettings()
     {
         BatchAccountKey = "my key",
         BatchAccountName = "my batch account",
         BatchAccountUrl = "my batch account url"
     },
     BatchStorageAccountSettings = new StorageAccountSettings()
     {
         StorageAccountKey = "my storage account key",
         StorageAccountName = "my storage account name"
     }
 };
```

In the example, we need to execute an R-script so in addition to specifying the path to the script, we specify an instance of *IDependencyResolver* (in this case named *ResolveRScriptDependencies*). 
This is a custom class, implementing *IDependencyResolver* which returns a list containing the paths to the files that the RScript we want to
execute depends on.

Similarly we specify an implementation of *IPrepareInputFiles* (PrepareInputUsing(...)) which creates the input files locally and then returns a list containing the paths to the created files.

The actual commands that we will ask Azure Batch to execute depend on the operating system family of the machines that we select when
invoking *ConfigureMachines(...)*, but the structure is as follows:

``` 
<executable or script name> <input file name> <additional argument 1> <additional argument 2> ...
```
Where <additional argument 1> and <additional argument 2> are added using the 
*.WithAdditionalScriptExecutionArgument(arg)* method.

When done, we invoke *StartBatch()* and wait for the batch to finish.

**Watts.Azure** will monitor the job and print some information during the execution. Right now it only reports the current status 
(how many nodes are active, running or finished) 
in a flat list format. More are implemented, but not available through the Fluent API (feel free to create a pull request :-)).

*If the job already exists* (e.g. because the application that started the job crashed) **Watts.Azure** will realize this and will simply start monitoring it.

You can control a lot of different settings through the Fluent API. This will be documented elsewhere.

# Some more examples

Here are a few examples, just to show you how **Watts.Azure** is used:

## Example 1 (R code in Windows Server 2012 R2)
The following runs some R code on a single node running Windows Server 2012 R2.
A single input file, named "inputFile1.txt" is created, which is then passed as an argument to the RScript when it executes in Batch.
Note that the R script in this case is hard-coded to read a file by that name. It is also passed to the script and could be read
with 
```
args = commandArgs(trailingOnly = TRUE)
filename <- args[1]
```
 
The code uses the OutputHelper application to get the output from the execution (stdout and stderr). OutputHelper works by uploading a file containing the output to the batch account linked to the batch account.
The pool and job are explicitly named. If not specified, they default to "BatchPool" and "BatchJob".
```cs 
BatchOutputContainer outputContainer = new BatchOutputContainer(this.environment.GetBatchStorageConnectionString());
AzureBlobStorage outputStorage = AzureBlobStorage.Connect(this.environment.GetBatchStorageConnectionString(), outputContainer.Name);
string relativePathToOutputHelper =
           "..\\..\\..\\..\\Watts.Azure.Common\\Watts.Azure.Common.OutputHelper\\bin\\Debug\\Watts.Azure.Common.OutputHelper.exe"; 

// Delete the output container if it already exists (it will be re-created).
outputStorage.DeleteContainerIfExists();

var builder = BatchBuilder.
    InPredefinedEnvironment(this.environment)
    .ResolveDependenciesUsing(new NetFrameworkDependencies(relativePathToOutputHelper))
    .WithPoolSetup(new BatchPoolSetup()
    {
        PoolId = "ExecuteBatchIntegrationTestsWindows",
        JobId = "RunSimpleRScriptOnWindows"
    });

var batchExecution = builder.ConfigureMachines(
    AzureMachineConfig.Small().WindowsServer2012R2().Instances(1)
    )
    .PrepareInputUsing(
        PrepareInputFiles.UsingFunction(() =>
        {
            string inputFile = "inputFile1.txt";
            File.WriteAllLines(inputFile, new string[] { "inputtest" });

            return new List<string>() { inputFile };
        }))
    .DontSaveStatistics()
    .UploadOutputTo(outputContainer)
    .ExecuteRCode(new string[]
    {
        "test <- read.csv2(\"inputFile1.txt\")",
        "print(test)"
    })
    .GetBatchExecution();

batchExecution.StartBatch().Wait();
```
NetFrameworkDependencies is a built-in *IDependencyResolver* that can be used to get all dependencies for a .NET assembly. It takes a folder 
path and returns all \*.config and \*.dll files, excluding files that contain the substring *.vshost*. If you don't need all files, you can implement 
your own *IDependencyResolver*.
The above code uses a delegate version of *IPrepareInputFiles*, and simply passes a delegate that prepares the files 
(PrepareInputFiles.UsingFunction(() => { ... }).

The example also specifically states that statistics for the execution should not be saved. If you instead invoke *SaveStatistics()* **Watts.Azure** will 
create a table storage in the Batch Storage Account (if it doesn't already exist) and insert an entity containing information about the execution 
(how long did it take, pool id, job id, number of nodes, number of tasks, etc.).

In this case, since it's a very simple piece of R code, the code is specified using a delegate rather than pointing to a file containing the R script 
(ExecuteRCode(new string[] {...})). In the background, this causes the given strings to be 
written to a file named main.R (with a guid appended after main), which is then uploaded to the 'application' blob.

## Example 2 (Combining multiple executables).
At some point, you may want to execute multiple scripts or executables, one after the other. For instance, one R script might do some machine learning stuff, save some output, and then a C# program takes the output and stores it somewhere.

**Watts.Azure** allows you to do that, by creating a *HybridBatch*. An example:

```cs
// Define an output blob to put the batch output in, so that we can download it after the execution
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
    .InPredefinedEnvironment(this.environment)
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

// Create a hybrid batch that executes the first batch followed by the second.
var hybridExecution = HybridBatchExecution
    .First(batchExecution)
    .Then(secondBatch)
    .GetCombinedBatchExecutionBase();

// ACT
// Start it and wait for it to finish.
hybridExecution
    .StartBatch()
    .Wait();

// Get the output
var hybridBatchOutput = hybridExecution.GetExecutionOutput();
```

## The pool and cleanup.
Unless specifically stated, **Watts.Azure** will clean up after the execution, by deleting the blob containers named *application(generated guid appended)* 
and *input(generated guid appended)* it creates to upload files to batch. It will also delete the job and the pool.

If you want **Watts.Azure** to **NOT** clean up, invoke ```DoNotCleanUpAfter()``` right before you invoke e.g. *ExecuteRScript*, *ExecuteRCode* or *RunExecutable*.

# **NOTE ON RUNNING R IN AZURE BATCH:**
The machines offered in Azure do not come with R pre-installed so you must do one of two things, depending on the O/S you're using.
  
  * **WINDOWS:** You must add an *Application package* to your batch account containing the R installation. I'm sure there are many ways of doing it, 
  but one that I've tested is to zip your entire R folder (typically C:\Program Files\R\R-x.y.z on a Windows system). You simply zip the whole 
  folder and add it through the Azure portal (Details [here](https://docs.microsoft.com/en-us/azure/batch/batch-application-packages)). 
  Remember to give it a version and make the one you want to use the *Default version*. 
  The application package will reside on the storage account linked with your batch account. 
  **IMPORTANT:** You must name the application package 'R' when you create it in the Azure Portal.
  **ALSO IMPORTANT:** The default version in **Watts.Azure**, if you do nothing, is R version 3.3.2. 
  If you've uploaded a different version in your application package, make sure you invoke *UseRVersion(string version)* (e.g. UseRVersion("3.4.1"))
  somewhere after your invocation of *.ExecuteRScript(string scriptPath)*.
  * **LINUX** You don't need to do anything really. **Watts.Azure** will run *apt-get install -y r-base* on the node before 
  executing your script. You can, however, not currently select the version of R you want to run when running in Linux.

## Test project
In order to execute Integration Tests and Manual Tests in this project you will need to fill in various account information related to batch.

When you run the first test, a file will be generated in the root directory of the test project, named **testEnvironment.testenv**. The file contains a JSON object specifying various required settings and credentials.

**IMPORTANT**: This file is ignored by git (the pattern *.testenv) and will not be commited. Don't change that or rename the file, as that could mean you'd be uploading your Azure credentials to github. 
We will of course review any pull requests and make sure that noone does this by accident, but it's better to be completely sure.

Add your relevant credentials to the file (which contains a json object deserialized into TestEnvironmentConfig.cs when each Integration/Manual test starts).

In case you only want to run Integration/Manual tests relevant to Batch, you will only need to fill in the settings:
- BatchAccountName
- BatchAccountKey
- BatchAccountUrl
- StorageAccountName
- StorageAccountKey

To run Data Factory (Copy data) tests, fill in 
- SubscriptionId
- ActiveDirectoryTenantId
- AdfClientId
- ClientSecret
- StorageAccountName
- StorageAccountKey

The details of how to obtain the keys/secrets etc. are explained in the next section:

In addition to the above there's a single test of using Azure File Share to upload/download data. To execute those you need to fill in 
- FileshareConnectionString

# Azure Data Factory
We support Copy Table -> Table currently through the fluent interface, e.g.

Similarly to working with batch, you will need an environment that implement *IPredefinedDataCopyEnvironment*. 
Implementing this environment requires you to find the following information:
1. Your subscription id (find it through the [Azure portal](https://portal.azure.com)).
2. Application client id (explained below)
3. Application client secret (explained below)
4. Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
5. Create a data factory in your subscription and get the **Resource group name** of the resource group it resides in.
6. Get the name and key of a storage account to be used along with the data factory. This can be found through the portal by browsing to the storage account and selecting "Keys".

### Application client id / client secret
To run a data factory you will need to register an app with your Azure Active Directory. 

The app doesn't do anything, except provide you with credentials to authenticate against Azure AD. Read more here:
https://www.netiq.com/communities/cool-solutions/creating-application-client-id-client-secret-microsoft-azure-new-portal/

### Example
```cs
// Create the source and target tables
AzureTableStorage sourceTable = AzureTableStorage.Connect(
	environment.GetDataFactoryStorageAccountConnectionString(), 
	"SourceTableTest");
AzureTableStorage targetTable = AzureTableStorage.Connect(
	environment.GetDataFactoryStorageAccountConnectionString(), 
	"TargetTableTest");

// Create an authenticator.
var authentication = new AzureActiveDirectoryAuthentication(this.environment.SubscriptionId, new AppActiveDirectoryAuthenticationCredentials()
{
    ClientId = this.environment.AdfClientId,
    ClientSecret = this.environment.ClientSecret,
    TenantId = this.environment.ActiveDirectoryTenantId
});

// Delete both tables if they exist and populate the source table with some data
var deleted = sourceTable.DeleteIfExists();
deleted &= targetTable.DeleteIfExists();

if(deleted)
{
    // Sleep a minute to ensure that Azure has actually deleted the table
    Thread.Sleep(60000);
}

// Create some random entities and insert them to the source table
int numberOfEntities = 10;
sourceTable.Insert<TestEntity>(TestFacade.RandomEntities(numberOfEntities, Guid.NewGuid().ToString()));

// Perform the copy source->target	.
DataCopyBuilder
    .InDataFactoryEnvironment(environment)
    .UsingDataFactorySetup(environment.DataFactorySetup)
    .UsingDefaultCopySetup()
    .WithTimeoutInMinutes(20)
    .AuthenticateUsing(authentication)
    .CopyFromTable(sourceTable)
    .WithSourceQuery(null)
    .ToTable(targetTable)
    .ReportProgressToConsole()
    .StartCopy();
```
The entities have now been copied from source to target.
You can use the timestamp of entities to perform incremental loads.