# Introduction 

This project contains code for executing azure batches as well as various utilities that make it easier to do so.
The preferred way of interacting with the code in this project is by using the fluent interface in *Watts.Azure.Utils*.

The main class for executing batches is **BatchExecutionBase.cs** which takes care of the nitty-gritty details involved in executing something in 
Azure Batch.

You need an Azure batch account in order to do something with it: see https://azure.microsoft.com/en-us/services/batch/
You will need, as a minimum, the batch account name and batch account key in order to execute anything.


# Batch

The overall idea used in batch is the following:
* Upload all executable files to a blob (application blob).
* Upload all input files to another blob (input blob).
* Process the pool of tasks (run something on an input file), one on each node until completed.
* Place all output files in an output blob (output blob).

As nodes are added to the pool in batch, a *start task* is executed on them. This can be pretty much anything, i.e. run a command, install an application.
It's just a console command.

When nodes are ready, they a take task each and start processing it. Each task is the processing of one input file.
The input file is copied to the node and a command is run on the node, which executes a script or executable on that input file.
When it's done processing the task, it grabs a new one that hasn't been taken yet. 
This continues until all tasks have been processed.

# Code

As already mentioned *BatchExecutionBase.cs* is the central class for executing batches.
Various settings objects are neccessary in order to do this, all located in ./Batch/Objects. 
The *BatchAccount.cs* class is also central. This was inspired from the basic tutorial by Microsoft on how to execute things in batch
and modified to suit the project. It wraps the interaction with the batch account itself (i.e. create/delete pool, jobs, tasks, etc).

Once a pool is created and a batch is running, the class *AzureTaskMonitor.cs* is used to check what the state is and whether the batch has finished.
It polls the tasks and checks if all tasks have reached the state 'TaskState.Completed'.

In addition, there are various helper classes like *ConsoleCommandHelper.cs*, *Dependencies.cs* and *PrepareInputFiles.cs*, of which the two latter
are meant as a help from the outside, when consuming this project.

Finally a number of classes wrap Azure table storage, Azure file share, etc.
It has been neccessary to create a number of wrappers around Azure.Batch classes, as these do not implement interfaces. So in order to
properly mock them in unit tests, wrappers are there simply to add the interface. 

Unit and Integration tests can be found in *Watts.Azure.Common.Tests*.

