using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Domain.AuditLogs;
using SharedKernel;

namespace Application.AuditLogs.GetCurrentUserAuditLogs;

/// <summary>
/// Query to get paginated audit logs for the current user.
/// </summary>
public sealed record GetCurrentUserAuditLogsQuery : PagedQuery, IQuery<PagedResult<AuditLogListItemResponse>>
{
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
