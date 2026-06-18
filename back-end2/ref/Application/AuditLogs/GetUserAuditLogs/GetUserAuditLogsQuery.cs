using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Domain.AuditLogs;
using SharedKernel;

namespace Application.AuditLogs.GetUserAuditLogs;

/// <summary>
/// Query to get paginated audit logs for a specific user.
/// </summary>
public sealed record GetUserAuditLogsQuery : PagedQuery, IQuery<PagedResult<AuditLogListItemResponse>>
{
    /// <summary>
    /// The ID of the user to get audit logs for.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditAction? Action { get; init; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Sort by field (Timestamp, Action, EntityType).
    /// </summary>
    public string SortBy { get; init; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
