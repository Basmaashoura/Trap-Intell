using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Domain events for the Auditing domain.
    /// </summary>

    /// <summary>
    /// Raised when an action is recorded in the audit trail.
    /// </summary>
    public record AuditRecordedEvent(
        Guid AuditTrailId,
        Guid OrganizationId,
        Guid? UserId,
        AuditResourceType ResourceType,
        Guid ResourceId,
        AuditAction Action,
        AuditSeverity Severity,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised specifically when a critical severity audit log is recorded, to trigger alerts.
    /// </summary>
    public record CriticalAuditLogRecordedEvent(
        Guid AuditTrailId,
        Guid OrganizationId,
        AuditResourceType ResourceType,
        Guid ResourceId,
        AuditAction Action,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when an audit log is acknowledged by an admin.
    /// </summary>
    public record AuditLogAcknowledgedEvent(
        Guid AuditTrailId,
        Guid AcknowledgedBy,
        DateTime AcknowledgedAt,
        DateTime OccurredOn) : IDomainEvent;
}
