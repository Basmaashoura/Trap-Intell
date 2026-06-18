using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;

namespace Trap_Intel.Application.Auditing.Queries.GetCriticalAuditLogs;

internal sealed class GetCriticalAuditLogsQueryHandler : IRequestHandler<GetCriticalAuditLogsQuery, Result<PagedResult<AuditTrailDto>>>
{
    private readonly IAuditTrailRepository _auditRepository;

    public GetCriticalAuditLogsQueryHandler(IAuditTrailRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<PagedResult<AuditTrailDto>>> Handle(GetCriticalAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var reasonFilter = queryOptions.GetSearchTerm();

        var sortBy = ResolveSortBy(queryOptions.SortBy);
        var sortDirection = queryOptions.IsSortDescending()
            ? AuditTrailSortDirection.Desc
            : AuditTrailSortDirection.Asc;

        var (logs, totalCount) = await _auditRepository.GetCriticalEntriesPagedAsync(
            request.OrganizationId,
            pageNumber,
            pageSize,
            reasonFilter,
            sortBy,
            sortDirection
        );

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
            l.Timestamp
        )).ToList();

        var result = new PagedResult<AuditTrailDto>(dtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }

    private static AuditTrailSortBy ResolveSortBy(string? rawSortBy)
    {
        var normalizedSortBy = rawSortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "severity" => AuditTrailSortBy.Severity,
            "action" => AuditTrailSortBy.Action,
            "resourcetype" or "resource" => AuditTrailSortBy.ResourceType,
            _ => AuditTrailSortBy.Timestamp
        };
    }
}
