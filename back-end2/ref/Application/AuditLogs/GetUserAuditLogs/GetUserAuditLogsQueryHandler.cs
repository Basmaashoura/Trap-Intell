using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.Common.Sorting;
using Application.Extensions;
using Domain.AuditLogs;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.AuditLogs.GetUserAuditLogs;

/// <summary>
/// Handler for GetUserAuditLogsQuery.
/// Returns paginated list of audit logs for a specific user.
/// </summary>
internal sealed class GetUserAuditLogsQueryHandler : IQueryHandler<GetUserAuditLogsQuery, PagedResult<AuditLogListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetUserAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> Handle(
        GetUserAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.Id == query.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<PagedResult<AuditLogListItemResponse>>(UserErrors.NotFound(query.UserId));
        }

        IQueryable<AuditLog> auditLogsQuery = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == query.UserId);

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

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, GetUserAuditLogsQuery request)
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
