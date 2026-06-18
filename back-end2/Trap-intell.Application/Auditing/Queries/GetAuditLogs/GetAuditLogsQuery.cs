using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogs;

public sealed record AuditTrailDto(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    AuditAction Action,
    AuditResourceType ResourceType,
    Guid ResourceId,
    AuditSeverity Severity,
    string? IpAddress,
    string? UserAgent,
    DateTime Timestamp,
    string? Reason = null,
    bool IsAcknowledged = false,
    bool IsArchived = false,
    Guid? AcknowledgedBy = null,
    DateTime? AcknowledgedAt = null,
    IReadOnlyList<ComplianceStandard>? ComplianceStandards = null
);

public sealed record GetAuditLogsQuery(
    Guid OrganizationId,
    GlobalQueryOptions? Query = null,
    AuditAction? Action = null,
    AuditResourceType? ResourceType = null,
    AuditSeverity? Severity = null,
    Guid? UserId = null,
    string? IpAddress = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ComplianceStandard? Standard = null,
    bool IncludeArchived = false,
    bool? IsAcknowledged = null,
    string? ReasonContains = null,
    AuditTrailSortBy SortBy = AuditTrailSortBy.Timestamp,
    AuditTrailSortDirection SortDirection = AuditTrailSortDirection.Desc
) : IRequest<Result<PagedResult<AuditTrailDto>>>;
