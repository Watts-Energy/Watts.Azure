namespace Watts.Azure.Common.Interfaces.General
{
    /// <summary>
    /// Log interface resembling log4net's ILog, but containing only a few simple methods.
    /// </summary>
    public interface ILog
    {
        void Debug(string statement);

        void Info(string statement);

        void Error(string statement);

        void Fatal(string statement);
    }
}