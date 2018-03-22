# Introduction

Watts.Azure exists primarily to make working with Azure Batch from .NET (especially to execute R code) easier. We found it needlessly cumbersome using the existing APIs and that it required you to write a lot of boilerplate code.
We want to create a simple interface to Azure Batch for those of us who do not need to know all the details but would like to take advantage of the massive potential for scaling compute that it offers.

**Watts.Azure** provides utilities to e.g. run parallel computations implemented in, for instance, *R*, *C#* or *python* in [Azure Batch](https://azure.microsoft.com/en-us/services/batch/).

In addition, it contains utilities to make aspects of working with [Azure Data Factory](https://azure.microsoft.com/en-us/services/data-factory/), [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/), 
[Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/), [Azure File Storage](https://azure.microsoft.com/en-us/services/storage/files/), [Azure Data Lake Store](https://azure.microsoft.com/en-us/services/data-lake-store/) and [Azure Service Bus Topics](https://azure.microsoft.com/en-us/services/service-bus/) easier.

Watts.Azure provides, among other things, a fluid interface that makes it simple to work with Azure Batch from .NET without dealing with all the low-level details. 
It's by no means a complete suite of tools to work with Azure, but it's a starting point. Any contributions that make it more complete are extremely welcome!

It also makes it easy to set up backup or data migrations in Azure Table Storage, by using the fluent interface for the [Azure Data Factory Copy Data activity](https://docs.microsoft.com/en-us/azure/data-factory/data-factory-data-movement-activities). In particular, it is convenient for creating backups, which can even be incremental, where you only copy the data that is not already present in an existing backup storage.
There is a specific utility exactly for this purpose. It is described in the section [Azure Table Storage Backup](#azure-table-storage-backup).

Some tools for working directly with Azure Table Storage, Azure Blob Storage, Azure Service Bus Topics and Azure Data Factory are also implemented.

Be sure to check out the Issues section and feel free to add feature suggestions.

The next section contains a brief introduction to Azure Batch, along with some code examples of how to use Watts.Azure to simplify its use.
The subsequent sections describe the utilities for using the [Azure Data Factory functionality in Watts.Azure](#azure-data-factory), and briefly describe the [Azure Service Bus topology helper](#azure-service-bus-topology), which allows you to automatically scale topics beyond the built-in limit of 2000 subscribers per topic.

**NOTE**: The current compute nodes in Azure support .NET framework up to 4.5.2. There's currently no way to install higher versions without
requiring a restart of the virtual machines in the Batch pool. If you're having trouble with executing your .NET executable in Batch, make sure you're targeting a version
<= 4.5.2 of the .NET framework.

Read on for some examples of how to use Watts.Azure.

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

**What does Watts.Azure solve in relation to using Azure Batch?** 
It provides a layer of abstraction that let's you focus on the 
basic things needed to execute something in Azure Batch, which are:
- Prepare input files that contain the data for each task to execute in Batch,
- Locate all files that are necessary for executing the executable or script and
- Construct the command that each node should run on its input file.

And that's it. Everything else, meaning all communication with the Azure Batch account, upload of executables and input files, monitoring of the job and
cleaning up afterwards, is handled by Watts.Azure.

The following shows an example of how Watts.Azure can be used to run an R-script in Azure Batch.
It executes an R-script named *myScript.R* on a single Windows Server R2 box in Azure Batch. In real life scenarios, you'd obviously want to perform this on more than a single node.

**IMPORTANT NOTE:** If you want to run R in Azure Batch on VMs running Windows, a tiny amount of legwork is neccesary: See the section [**NOTE ON RUNNING R IN AZURE BATCH**](#note-on-running-r-in-azure-batch):

```cs
 var batch = BatchBuilder
    .InEnvironment(environment)
    .ResolveDependenciesUsing(new ResolveRScriptDependencies("./"))
    .WithDefaultPoolSetup()
    .ConfigureMachines(AzureMachineConfig.StandardD1_V2().Instances(numberOfNodes))
    .PrepareInputUsing(filePreparation)
    .ReportProgressToConsole()
    .SetTimeoutInMinutes(timeout)
    .ExecuteRScript("./myScript.R")
    .WithAdditionalScriptExecutionArgument(argument1)
    .WithAdditionalScriptExecutionArgument(argument2)
    .GetBatchExecution()
    .StartBatch();

 batch.Wait();
```

Here, **environment** is a class implementing *IBatchEnvironment*, specifying the credentials for the batch account to use, and the
credentials of the storage account where it's to put the application, input and (possibly) output files.
You can create your own environment class, by implementing IBatchEnvironment. or you can simply use:

```cs
 IBatchEnvironment env = new BatchEnvironment()
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
This can be a custom class that implements the interface, or you can simply pass it a delegate using ```PrepareInputUsing(p => { return filePaths; }```.

The actual commands that we will ask Azure Batch to execute depend on the operating system family of the machines that we select when
invoking *ConfigureMachines(...)*, but the basic structure is as follows:

``` 
<executable or script name> <input file name> <additional argument 1> <additional argument 2> ...
```
Where <additional argument 1> and <additional argument 2> are added using the 
*.WithAdditionalScriptExecutionArgument(arg)* method.

When done, we invoke *StartBatch()* and wait for the batch to finish.

Watts.Azure will monitor the job and print some information during the execution. You can select various ways that it should report the status while the task is running (e.g. flat list, summary, etc).

*If the job already exists* (e.g. because the application that started the job crashed) Watts.Azure will realize this and will simply start monitoring it.

You can control a lot of different settings through the Fluent API. This is documented in the Wiki.

# Some more examples

Here are a few examples, just to show you how Watts.Azure is used:

## Example 1 (R code in Windows Server 2012 R2)
The following runs some R code on a single node running Windows Server 2012 R2.
A single input file, named "inputFile1.txt" is created, which is then passed as an argument to the RScript when it executes in Batch.
Note that the R script in this case is hard-coded to read a file by that name. It is also passed to the script and could be read
with 
```
args = commandArgs(trailingOnly = TRUE)
filename <- args[1]
```
 
The pool and job are explicitly named in the following example. If not specified, they default to "BatchPool" and "BatchJob".
```cs 
var builder = BatchBuilder.
    InEnvironment(this.environment)
	.NoDependencies()
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
    .DontSaveStatistics() // Specify that we don't want an entity containing statistics about the run saved.
    .DownloadOutput() // Specify that Watts.Azure should download the stdout and stderr of the tasks.
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
(```PrepareInputFiles.UsingFunction(() => { /* return files here */}```).

The example also specifically states that statistics for the execution should not be saved. If you instead invoke *SaveStatistics()* Watts.Azure will 
create a table storage in the Batch Storage Account (if it doesn't already exist) and insert an entity containing information about the execution 
(how long did it take, pool id, job id, number of nodes, number of tasks, etc.).

In this case, since it's a very simple piece of R code, the code is specified using a delegate rather than pointing to a file containing the R script 
(ExecuteRCode(new string[] {...})). In the background, this causes the given strings to be 
written to a file named main.R (with a guid appended after main), which is then uploaded to the 'application' blob.

## Example 2 (Combining multiple executables).
At some point, you may want to execute multiple scripts or executables, one after the other. For instance, one R script might do some machine learning stuff, save some output, and then a C# program takes the output and stores it somewhere.

Watts.Azure allows you to do that, by creating a *HybridBatch*. An example:

```cs
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
.DownloadOutput()
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
Unless specifically stated, Watts.Azure will clean up after the execution, by deleting the blob containers named *application(generated guid appended)* 
and *input(generated guid appended)* it creates to upload files to batch. It will also delete the job and the pool.

If you want Watts.Azure to **NOT** clean up, invoke ```DoNotCleanUpAfter()``` right before you invoke e.g. *ExecuteRScript*, *ExecuteRCode* or *RunExecutable*.

# **NOTE ON RUNNING R IN AZURE BATCH:**
The machines offered in Azure do not come with R pre-installed so you must do one of two things, depending on the O/S you're using.
  
  * **WINDOWS:** You must add an *Application package* to your batch account containing the R installation. I'm sure there are many ways of doing it, 
  but one that I've tested is to zip your entire R folder (typically C:\Program Files\R\R-x.y.z on a Windows system). You simply zip the whole 
  folder and add it through the Azure portal (Details [here](https://docs.microsoft.com/en-us/azure/batch/batch-application-packages)). 
  Remember to give it a version and make the one you want to use the *Default version*. 
  The application package will reside on the storage account linked with your batch account. 
  **IMPORTANT:** You must name the application package 'R' when you create it in the Azure Portal.
  **ALSO IMPORTANT:** The default version in Watts.Azure, if you do nothing, is R version 3.3.2. 
  If you've uploaded a different version in your application package, make sure you invoke *UseRVersion(string version)* (e.g. UseRVersion("3.4.1"))
  somewhere after your invocation of *.ExecuteRScript(string scriptPath)*.
  * **LINUX** You don't need to do anything really. Watts.Azure will run *apt-get install -y r-base* on the node before 
  executing your script. You can, however, not currently select the version of R you want to run when running in Linux.

  **Install R-packages in your R-script running in Azure Batch**
  In order to install packages when running R through Watts.Azure, you must let R know where packages are to be placed.
  Similarly, when using them you must let R know where to load them from.
  The following code snippet shows how it can be done.

  ```r
# Set the repository to get packages from
repository <- "https://cloud.r-project.org/"

# Set the local folder (relative to the current working directory) to download packages to
localPackagesFolderName <- "rpackages"

# List the packages you want to install
list.of.packages <- c("digest", "zoo")

# Get a list of packages that are not currently installed
new.packages <- list.of.packages[!(list.of.packages %in% installed.packages()[, "Package"])]
if (length(new.packages)) install.packages(new.packages, repos = repository, lib = localPackagesFolderName)

# Tell R where to look for packages...
.libPaths(localPackagesFolderName)

# Import the packages
require(digest, lib.loc = localPackagesFolderName)
require(zoo, lib.loc = localPackagesFolderName)
```

# Azure Data Factory
We currently support Copy Table -> Table and Table -> DataLake, through the fluent interface.

Similarly to working with batch, you will need an environment that implements *IDataCopyEnvironment*. 
Implementing this environment requires you to find the following information:
1. Your subscription id (find it through the [Azure portal](https://portal.azure.com)).
2. Application client id (explained below)
3. Application client secret (explained below)
4. Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
5. Create a data factory in your subscription and get the **Resource group name** of the resource group it resides in.
6. Get the name and key of a storage account to be used along with the data factory. This can be found through the portal by browsing to the storage account and selecting "Keys".

### Application client id and client secret
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

You can use the timestamp of entities to perform incremental loads by e.g. specifying a query string as argument to WithSourceQuery(string query).
E.g. when moving data from Table Storage you could write ```Timestamp gt datetime'{lastBackupStarted.Value.ToIso8601()}'``` which selects
only entities from the source which have been modified since 'lastBackupStarted' (which would be a DateTime/DateTimeOffset). ```ToIso8601``` simply
ensures that the date is in a format Table Storage understands.

# Azure Table Storage Backup
The class ```TableStorageBackup.cs``` in Watts.Azure.Common.Backup makes it very easy to set up rolling backups of Azure Tables.
It is meant to be run frequently through e.g. an Azure Web job, so that it can constantly monitor the schedule and see if new backups need to be created
or incremental changes need to be transferred from the source table to the current backup.

You can use it to build backup pipelines that automatically switch between targets and maintain a rolling backup of the table, cleaning up copies that are older
than some configured value.

To do this, you need to provide a ```BackupSetup``` object, in turn containing any number of ```TableBackupSetup``` objects, each specifying a ```BackupSchedule```.
The backup schedule specifies three timespans: 
- Retention time: i.e. how long will any backup live,
- Incremental load frequency: (how often will incremental changes be copied from the source to the newest backup), and
- Switch target frequency: How long before we should switch to a new storage account.

The backup object will handle everything related to provisioning of resources in the target subscription and will create the resource group specified in the setup if it doesn't
already exist, the resource groups when needed and backup tables.

All storage accounts are placed under the configured resource group, and are given names according to the time they were created concatenated with a suffix you specify in the setup.

All this is described in more detail in the Wiki and there are integration tests you can read to get a feeling for the functionality.

## Azure Data Lake
Watts.Azure contains some utilities for interacting with Azure Data Lake.

Watts Azure uses Service Principal Authentication when authenticating towards Azure Data Lake and all you need to do is to provide the credentials. 
Specifically, it needs an instance of *IAzureActiveDirectoryAuthentication*, which must specify
- SubscriptionId (find it through the [Azure portal](https://portal.azure.com)).
- ResourceGroupName (only necessary if you're using it with Data Factory copy)
- Active Directory Tenant Id (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-howto-tenant)
- Application client id
- Application client secret

See the section [**Application client id and client secret**](#application-client-id-and-client-secret) for an explanation of how to obtain the client id.

**IMPORTANT NOTE** 
To communicate with Azure Data Lake Store you will need to give the application you're using to authenticate against it access to the data store (The app registration whose clientid/secret you've specified above).
To do this, go to the [Azure Portal](https://portal.azure.com) and 
- navigate to the Data Lake Store in question,
- select 'Data Explorer'
- click the root folder which you want to grant access to and then select 'Access' in the top menu
- click 'Add' and select the application
- Grant the application 'Read', 'Write' and 'Execute' permissions.
- Don't forget to save.

## Azure Service Bus Topology
Azure Service Bus Topics have a limit on the number of subscriptions, which is 2000 currently (2017). 

If you want to scale beyond that you will need to create a 'tree' of topics, where the root topic auto-forwards messages to the sub-topics.
Adding one level lets you scale to 2000 * 2000 subscribers, since each of the 2000 children can support 2000 subscriptions.

To make setting this up easier, we've added ```AzureServiceBusTopology```. It allows you to simply specify how many subscriptions you need to support, and it can set everything up for you.
Lets say you wanted to support 10 000 subscriptions and wanted a maximum of 500 subscriptions per topic, you could do the following:

```cs
 int maxSubscriptionsPerTopic = 500;
 int requiredNumberOfSubscriptions = 10000;
 var topology = new AzureServiceBusTopology("servicebus-namespace", "topic-name", new AzureServiceBusManagement(this.auth), AzureLocation.NorthEurope, maxSubscriptionsPerTopic);

 topology.ReportOn(Console.WriteLine); // Output messages will be written to console
 topology.GenerateBusTopology(requiredNumberOfSubscriptions); // It will generate the topology needed to support this many subscriptions
 topology.Emit(); // The topology will be created through the management API. This can take a while...
```
and to delete the topics and subscriptions, call
```cs
topology.Destroy();
```


## Test project
In order to execute Integration Tests and Manual Tests in this project you will need to fill in various account information related to batch.

When you run the first test, a file will be generated in the one level above the root directory of Watts.Azure, named **testEnvironment.testenv**. The file contains a JSON object specifying various required settings and credentials.

**IMPORTANT**: This file is ignored by git (the pattern *.testenv) and will not be commited, even if you copy it into the repository folder. It is placed outside the repository to ensure that you
do not accidentally commit the file.

Add your relevant credentials to the file (which contains a json object deserialized into TestEnvironmentConfig.cs when each Integration/Manual test starts).

In case you only want to run Integration/Manual tests relevant to Batch, you will only need to fill in the settings:
- BatchAccountName
- BatchAccountKey
- BatchAccountUrl
- StorageAccountName
- StorageAccountKey

To run Data Factory (Copy data) and Data Lake tests, fill in 
- SubscriptionId
- Credentials.TenantId
- Credentials.ClientId
- Credentials.ClientSecret
- StorageAccountName
- StorageAccountKey

and additionally 
- DataLakeStoreName

To run Azure Service Bus tests fill
- SubscriptionId
- ResourceGroupName
- NamespaceName
- Location
- Credentials.TenantId
- Credentials.ClientId
- Credentials.ClientSecret

if you want to run tests that involve Azure Data Lake.
Both the **DataCopyEnvironment** and **DataLakeEnvironment** have the above settings, except *DataLakeStoreName* which is exclusive to Data lake (obviously).
The settings are replicated, because it is not given that your Data lake store and Data factory share these settings. 
Your Data lake store could even be in a different subscription than your Fata factory.

The details of how to obtain the keys/secrets etc. are explained in section [Application client id and client secret](#application-client-id-and-client-secret)

In addition to the above there's a single test of using Azure File Share to upload/download data. To execute that you need to fill in 
- FileshareConnectionString
which should be the connection string to the storage account that has the fileshare you would like to test against.