using MediatR;
using System.Linq;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogsSummary;

internal sealed class GetAuditLogsSummaryQueryHandler : IRequestHandler<GetAuditLogsSummaryQuery, Result<AuditLogsSummaryDto>>
{
    private readonly IAuditTrailRepository _auditTrailRepository;

    public GetAuditLogsSummaryQueryHandler(IAuditTrailRepository auditTrailRepository)
    {
        _auditTrailRepository = auditTrailRepository;
    }

    public async Task<Result<AuditLogsSummaryDto>> Handle(GetAuditLogsSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            return Result.Failure<AuditLogsSummaryDto>(
                Error.Custom("Auditing.InvalidDateRange", "EndDate cannot be earlier than StartDate."));
        }

        var top = request.Top < 1 ? 5 : Math.Min(request.Top, 20);

        var summary = await _auditTrailRepository.GetSummaryAsync(
            organizationId: request.OrganizationId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            includeArchived: request.IncludeArchived,
            top: top);

        var response = new AuditLogsSummaryDto(
            TotalEvents: summary.TotalEvents,
            AcknowledgedEvents: summary.AcknowledgedEvents,
            UnacknowledgedEvents: summary.UnacknowledgedEvents,
            ArchivedEvents: summary.ArchivedEvents,
            EventsBySeverity: summary.EventsBySeverity.Select(x => new SeveritySummaryDto(x.Severity, x.Count)).ToList(),
            TopActions: summary.TopActions.Select(x => new ActionSummaryDto(x.Action, x.Count)).ToList(),
            TopResourceTypes: summary.TopResourceTypes.Select(x => new ResourceTypeSummaryDto(x.ResourceType, x.Count)).ToList());

        return Result.Success(response);
    }
}