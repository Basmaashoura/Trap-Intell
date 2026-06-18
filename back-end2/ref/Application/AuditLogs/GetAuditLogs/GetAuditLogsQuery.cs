using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Domain.AuditLogs;
using SharedKernel;

namespace Application.AuditLogs.GetAuditLogs;

/// <summary>
/// Query to get paginated audit logs with filtering and sorting.
/// </summary>
public sealed record GetAuditLogsQuery : PagedQuery, IQuery<PagedResult<AuditLogListItemResponse>>
{
    /// <summary>
    /// Search term to filter audit logs.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditAction? Action { get; init; }

    /// <summary>
    /// Filter by entity type (e.g., "User", "Role", "Account").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Sort by field (Timestamp, Action, EntityType, UserEmail).
    /// </summary>
    public string SortBy { get; init; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
