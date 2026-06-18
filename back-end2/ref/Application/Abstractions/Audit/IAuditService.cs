using Domain.AuditLogs;

namespace Application.Abstractions.Audit;

/// <summary>
/// Service for recording audit logs.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an audit log entry.
    /// </summary>
    Task LogAsync(
        AuditAction action,
        string entityType,
        string? entityId,
        string? entityName = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an audit log for authentication events.
    /// </summary>
    Task LogAuthenticationAsync(
        AuditAction action,
        Guid? userId,
        string? userEmail,
        string? description = null,
        CancellationToken cancellationToken = default);
}
