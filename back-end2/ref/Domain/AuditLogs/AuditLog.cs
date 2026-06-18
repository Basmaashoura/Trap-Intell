using SharedKernel;

namespace Domain.AuditLogs;

/// <summary>
/// Represents an audit log entry for tracking system changes.
/// Immutable - once created, cannot be modified.
/// </summary>
public sealed class AuditLog : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user who performed the action.
    /// Null for system-generated events.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Email of the user (snapshot at time of action).
    /// </summary>
    public string? UserEmail { get; private set; }

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public AuditAction Action { get; private set; }

    /// <summary>
    /// The entity type affected (e.g., "User", "Role", "Account").
    /// </summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>
    /// The ID of the affected entity.
    /// </summary>
    public string? EntityId { get; private set; }

    /// <summary>
    /// Friendly name/description of the affected entity.
    /// </summary>
    public string? EntityName { get; private set; }

    /// <summary>
    /// JSON representation of the old values (before change).
    /// </summary>
    public string? OldValues { get; private set; }

    /// <summary>
    /// JSON representation of the new values (after change).
    /// </summary>
    public string? NewValues { get; private set; }

    /// <summary>
    /// Additional context or notes about the action.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Correlation ID for tracking related actions.
    /// </summary>
    public string? CorrelationId { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Create(
        Guid? userId,
        string? userEmail,
        AuditAction action,
        string entityType,
        string? entityId,
        string? entityName = null,
        string? oldValues = null,
        string? newValues = null,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues,
            NewValues = newValues,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId
        };
    }
}
