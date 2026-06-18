namespace Trap_Intel.Domain.Reporting
{
    /// <summary>Report type enumeration.</summary>
    public enum ReportType
    {
        Technical = 0,
        SummaryCEO = 1,
        Logs = 2,
        Custom = 3
    }

    /// <summary>Report status enumeration.</summary>
    public enum ReportStatus
    {
        Draft = 0,
        Generated = 1,
        Reviewed = 2,
        Sent = 3
    }

    /// <summary>Report export format enumeration.</summary>
    public enum ReportFormat
    {
        PDF = 0,
        CSV = 1,
        HTML = 2,
        JSON = 3
    }

    /// <summary>Export status enumeration.</summary>
    public enum ExportStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }
}
