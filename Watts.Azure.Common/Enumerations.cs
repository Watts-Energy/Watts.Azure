namespace Watts.Azure.Common
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Error = 2,
        Fatal = 3
    }

    public enum ReportPoolStatusFormat
    {
        FlatList,
        GroupedByState,
        Summary,
        Silent
    }

    public enum OperatingSystemFamily
    {
        Linux,
        Windows
    }
}