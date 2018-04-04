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

    public enum AzureLocation
    {
        WestUs,
        WestUs2,
        WestCentralUs,
        SouthCentralUs,
        NorthCentralUs,
        EastUs,
        EastUs2,
        BrazilSouth,
        UkSouth,
        UkWest,
        NorthEurope,
        FranceCentral,
        FranceSouth,
        WestEurope,
        GermanyNorthEast,
        GermanyCentral,
        WestIndia,
        CentralIndia,
        SouthIndia,
        ChinaNorth,
        ChinaEast,
        EastAsia,
        SouthEastAsia,
        KoreaCentral,
        KoreaSouth,
        JapanEast,
        JapanWest,
        AustraliaEast,
        AustraliaCentral,
        AustraliaCentral2,
        AustraliaSouthEast
    }

    public enum MessageFormat
    {
        Json,
        Xml
    }

    public enum ScaleMode
    {
        Horizontally,
        Vertically
    }

    public enum BackupStatus
    {
        Success,
        Failure,
        InProgress,
        Unknown
    }

    public enum BackupMode
    {
        Full,
        Incremental,
    }

    public enum BackupReturnCode
    {
        BackupToExistingContainerDone,
        BackupToNewContainerDone,
        Nop,
        Error,
    }
}