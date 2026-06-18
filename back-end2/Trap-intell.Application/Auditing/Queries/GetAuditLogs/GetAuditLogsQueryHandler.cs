using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogs;

internal sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditTrailDto>>>
{
    private readonly IAuditTrailRepository _auditRepository;

    public GetAuditLogsQueryHandler(IAuditTrailRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<PagedResult<AuditTrailDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var reasonFilter = string.IsNullOrWhiteSpace(request.ReasonContains)
            ? queryOptions.GetSearchTerm()
            : request.ReasonContains;

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            return Result.Failure<PagedResult<AuditTrailDto>>(
                Error.Custom("Auditing.InvalidDateRange", "EndDate cannot be earlier than StartDate."));
        }

        var (logs, totalCount) = await _auditRepository.SearchPagedAsync(
            organizationId: request.OrganizationId,
            userId: request.UserId,
            action: request.Action,
            resourceType: request.ResourceType,
            severity: request.Severity,
            ipAddress: request.IpAddress,
            startDate: request.StartDate,
            endDate: request.EndDate,
            standard: request.Standard,
            includeArchived: request.IncludeArchived,
            pageNumber: pageNumber,
            pageSize: pageSize,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            isAcknowledged: request.IsAcknowledged,
            reasonContains: reasonFilter);

        var dtos = logs.Select(l => new AuditTrailDto(
            l.Id,
            l.OrganizationId,
            l.UserId,
            l.Action,
            l.ResourceType,
            l.ResourceId,
            l.Severity,
            l.IpAddress,
            l.UserAgent,
            l.Timestamp,
            l.Reason,
            l.IsAcknowledged,
            l.IsArchived,
            l.AcknowledgedBy,
            l.AcknowledgedAt,
            l.ComplianceStandards
        )).ToList();

        var result = new PagedResult<AuditTrailDto>(dtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }
}
