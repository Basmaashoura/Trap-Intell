using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.Common.Sorting;
using Application.Extensions;
using Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.AuditLogs.GetCurrentUserAuditLogs;

/// <summary>
/// Handler for GetCurrentUserAuditLogsQuery.
/// Returns paginated list of audit logs for the current user.
/// </summary>
internal sealed class GetCurrentUserAuditLogsQueryHandler : IQueryHandler<GetCurrentUserAuditLogsQuery, PagedResult<AuditLogListItemResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserAuditLogsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> Handle(
        GetCurrentUserAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<AuditLog> auditLogsQuery = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.UserId);

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

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, GetCurrentUserAuditLogsQuery request)
    {
        if (request.Action.HasValue)
        {
            query = query.Where(a => a.Action == request.Action.Value);
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
