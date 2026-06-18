using Domain.AuditLogs;

namespace Application.AuditLogs.Common;

/// <summary>
/// Response DTO for AuditLog information.
/// </summary>
public sealed record AuditLogResponse
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName => Action.ToString();
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? EntityName { get; init; }
    public string? Description { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Detailed response DTO for AuditLog including old/new values.
/// </summary>
public sealed record AuditLogDetailResponse
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName => Action.ToString();
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? EntityName { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Description { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Simplified response for audit log lists.
/// </summary>
public sealed record AuditLogListItemResponse
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityName { get; init; }
    public string? Description { get; init; }
    public DateTime Timestamp { get; init; }
}
