namespace Watts.Azure.Utils.Interfaces.Batch
{
    using System;
    using Common.Batch.Objects;
    using Common.Interfaces.General;
    using Watts.Azure.Utils.Helpers.Batch;

    public interface IBatchCreationWithInputPreparation
    {
        IBatchCreationWithInputPreparation ReportingProgressUsing(
            Action<string> progressDelegate);

        IBatchCreationWithInputPreparation ReportProgressToConsole();

        IBatchCreationWithInputPreparation UploadOutputTo(BatchOutputContainer outputContainer);

        IBatchCreationWithInputPreparation LogTo(ILog log);

        IBatchCreationWithInputPreparation SaveStatistics();

        IBatchCreationWithInputPreparation DontSaveStatistics();

        IBatchCreationWithInputPreparation SetTimeoutInMinutes(int minutes);

        RBatchCreation ExecuteRScript(string scriptFileName);

        RBatchCreation ExecuteRCode(string[] code);

        ExecutableBatchCreation RunExecutable(string executableFilePath);
    }
}