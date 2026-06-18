namespace Trap_Intel.Domain.Auditing;

public sealed record AuditSeveritySummary(AuditSeverity Severity, int Count);

public sealed record AuditActionSummary(AuditAction Action, int Count);

public sealed record AuditResourceTypeSummary(AuditResourceType ResourceType, int Count);

public sealed record AuditLogsSummarySnapshot(
    int TotalEvents,
    int AcknowledgedEvents,
    int UnacknowledgedEvents,
    int ArchivedEvents,
    IReadOnlyList<AuditSeveritySummary> EventsBySeverity,
    IReadOnlyList<AuditActionSummary> TopActions,
    IReadOnlyList<AuditResourceTypeSummary> TopResourceTypes
);