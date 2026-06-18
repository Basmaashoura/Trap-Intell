namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Enums for the Auditing domain.
    /// </summary>

    /// <summary>
    /// Actions that can be audited in the system.
    /// </summary>
    public enum AuditAction
    {
        Create = 0,
        Update = 1,
        Delete = 2,
        View = 3,
        Deploy = 4,
        Approve = 5,
        Reject = 6,
        Cancel = 7,
        Activate = 8,
        Deactivate = 9,
        Suspend = 10,
        Resume = 11,
        Export = 12,
        Import = 13,
        Publish = 14,
        Archive = 15
    }

    /// <summary>
    /// Severity levels for audit events.
    /// </summary>
    public enum AuditSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    /// <summary>
    /// Resource types that can be audited.
    /// </summary>
    public enum AuditResourceType
    {
        User = 0,
        Organization = 1,
        Subscription = 2,
        Plan = 3,
        Invoice = 4,
        PaymentMethod = 5,
        Report = 6,
        Dashboard = 7,
        Recommendation = 8,
        Settings = 9,
        HoneyPot = 10,
        Role = 11
    }

    /// <summary>
    /// Compliance standards for audit trail tagging.
    /// </summary>
    public enum ComplianceStandard
    {
        None = 0,
        GDPR = 1,          // General Data Protection Regulation
        HIPAA = 2,         // Health Insurance Portability and Accountability Act
        SOC2 = 3,          // Service Organization Control 2
        ISO27001 = 4,      // Information Security Management
        PCI_DSS = 5,       // Payment Card Industry Data Security Standard
        CCPA = 6,          // California Consumer Privacy Act
        NIST = 7,          // National Institute of Standards and Technology
        FedRAMP = 8        // Federal Risk and Authorization Management Program
    }

    /// <summary>
    /// Sort fields available for audit trail listing queries.
    /// </summary>
    public enum AuditTrailSortBy
    {
        Timestamp = 0,
        Severity = 1,
        Action = 2,
        ResourceType = 3
    }

    /// <summary>
    /// Sort direction for audit trail listing queries.
    /// </summary>
    public enum AuditTrailSortDirection
    {
        Desc = 0,
        Asc = 1
    }
}
