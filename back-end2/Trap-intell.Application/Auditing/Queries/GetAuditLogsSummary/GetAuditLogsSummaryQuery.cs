using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogsSummary;

public sealed record AuditLogsSummaryDto(
    int TotalEvents,
    int AcknowledgedEvents,
    int UnacknowledgedEvents,
    int ArchivedEvents,
    IReadOnlyList<SeveritySummaryDto> EventsBySeverity,
    IReadOnlyList<ActionSummaryDto> TopActions,
    IReadOnlyList<ResourceTypeSummaryDto> TopResourceTypes
);

public sealed record SeveritySummaryDto(AuditSeverity Severity, int Count);

public sealed record ActionSummaryDto(AuditAction Action, int Count);

public sealed record ResourceTypeSummaryDto(AuditResourceType ResourceType, int Count);

public sealed record GetAuditLogsSummaryQuery(
    Guid OrganizationId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool IncludeArchived = true,
    int Top = 5
) : IRequest<Result<AuditLogsSummaryDto>>;