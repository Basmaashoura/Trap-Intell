using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Abstractions.Auditing;

public interface IAuditService
{
    Task RecordAsync(
        Guid organizationId,
        AuditResourceType resourceType,
        Guid resourceId,
        AuditAction action,
        AuditSeverity severity,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task RecordLoginAsync(Guid userId, Guid organizationId, bool isSuccess, string? failureReason = null, CancellationToken cancellationToken = default);

    Task RecordChangesAsync(
        Guid organizationId,
        AuditResourceType resourceType,
        Guid resourceId,
        AuditAction action,
        AuditSeverity severity,
        List<(string PropertyName, string? OldValue, string? NewValue)> changes,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
