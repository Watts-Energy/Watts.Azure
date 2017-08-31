namespace Watts.Azure.Common.Interfaces.General
{
    /// <summary>
    /// Log interface resembling log4net's ILog.
    /// </summary>
    public interface ILog
    {
        void Debug(string statement);

        void Info(string statement);

        void Error(string statement);

        void Fatal(string statement);
    }
}