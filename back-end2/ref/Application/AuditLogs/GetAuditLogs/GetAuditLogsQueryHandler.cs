using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.Common.Filtering;
using Application.Common.Sorting;
using Application.Extensions;
using Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.AuditLogs.GetAuditLogs;

/// <summary>
/// Handler for GetAuditLogsQuery.
/// Returns paginated list of audit logs with filtering and sorting.
/// </summary>
internal sealed class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> Handle(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<AuditLog> auditLogsQuery = _context.AuditLogs.AsNoTracking();

        // Apply search
        auditLogsQuery = auditLogsQuery.ApplySearch(AuditLogSearchConfiguration.Instance, query.SearchTerm);

        // Apply filters
        auditLogsQuery = ApplyFilters(auditLogsQuery, query);

        // Apply sorting
        auditLogsQuery = auditLogsQuery.ApplySort(AuditLogSortConfiguration.Instance, query.SortBy, query.SortDirection);

        // Project to response
        IQueryable<AuditLogListItemResponse> responseQuery = auditLogsQuery.Select(a => new AuditLogListItemResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            UserEmail = a.UserEmail,
            ActionName = a.Action.ToString(),
            EntityType = a.EntityType,
            EntityName = a.EntityName,
            Description = a.Description,
            Timestamp = a.Timestamp
        });

        // Paginate
        PagedResult<AuditLogListItemResponse> pagedResult = await responseQuery
            .ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);

        return Result.Success(pagedResult);
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, GetAuditLogsQuery request)
    {
        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (request.Action.HasValue)
        {
            query = query.Where(a => a.Action == request.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
            query = query.Where(a => a.EntityType.ToUpper() == request.EntityType.ToUpper());
#pragma warning restore CA1304, CA1311, CA1862
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            query = query.Where(a => a.EntityId == request.EntityId);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        return query;
    }
}
