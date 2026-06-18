using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Application.Abstractions.Alerts;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Alerts.Queries.GetAlerts;
using Trap_Intel.Application.Alerts.Queries.GetAlertById;
using Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Alerts.Services;

internal sealed class AlertQueryService : IAlertQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public AlertQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AlertDto>> GetAlertsAsync(
        Guid organizationId, 
        AlertStatus? status, 
        AlertSeverity? severity, 
        AlertType? type, 
        Guid? assignedUserId, 
        GlobalQueryOptions queryOptions,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var searchTerm = queryOptions.GetSearchTerm();

        var query = _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (type.HasValue)
            query = query.Where(a => a.AlertType == type.Value);

        if (assignedUserId.HasValue)
            query = query.Where(a => a.AssignedToUserId == assignedUserId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";

            query = query.Where(a =>
                EF.Functions.ILike(a.Title, pattern) ||
                EF.Functions.ILike(a.Description, pattern) ||
                EF.Functions.ILike(a.Source.SourceName!, pattern));
        }

        query = ApplySort(query, queryOptions.SortBy, queryOptions.IsSortDescending());

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AlertDto(
                a.Id,
                a.AlertType,
                a.Severity,
                a.Priority,
                a.Title,
                a.Status,
                a.Source.SourceType,
                a.Source.SourceName,
                a.AssignedToUserId,
                a.CreatedAt,
                a.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

            return new PagedResult<AlertDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<AlertDetailDto?> GetAlertByIdAsync(Guid organizationId, Guid alertId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.Id == alertId && a.OrganizationId == organizationId)
            .Select(a => new AlertDetailDto(
                a.Id,
                a.AlertType,
                a.Severity,
                a.Priority,
                a.Title,
                a.Description,
                a.Status,
                a.Source.SourceType,
                a.Source.SourceName,
                a.Source.SourceId,
                a.EscalationLevel,
                a.AssignedToUserId,
                a.AcknowledgedByUserId,
                a.AcknowledgedAt,
                a.ResolvedByUserId,
                a.ResolvedAt,
                a.Resolution,
                a.CreatedAt,
                a.UpdatedAt,
                a.Actions.Select(ac => new ActionDto(ac.ActionType.ToString(), ac.Description, ac.PerformedByUserId, ac.PerformedAt)).ToList(),
                a.Comments.Select(c => new CommentDto(c.Content, c.AuthorUserId, c.CreatedAt, c.IsInternal)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AlertDashboardStatisticsDto> GetDashboardStatisticsAsync(Guid organizationId, int lastNDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-lastNDays);

        var baseQuery = _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        var totalActive = await baseQuery
            .CountAsync(a => a.Status != AlertStatus.Resolved && a.Status != AlertStatus.Expired && a.Status != AlertStatus.FalsePositive, cancellationToken);

        var unacknowledged = await baseQuery
            .CountAsync(a => a.Status == AlertStatus.New, cancellationToken);

        var criticalUnresolved = await baseQuery
            .CountAsync(a => a.Severity == AlertSeverity.Critical && a.Status != AlertStatus.Resolved && a.Status != AlertStatus.Expired && a.Status != AlertStatus.FalsePositive, cancellationToken);

        var escalated = await baseQuery
            .CountAsync(a => a.EscalationLevel > EscalationLevel.Level1 && a.Status != AlertStatus.Resolved && a.Status != AlertStatus.Expired && a.Status != AlertStatus.FalsePositive, cancellationToken);

        var falsePositives = await baseQuery
            .CountAsync(a => a.Status == AlertStatus.FalsePositive && a.CreatedAt >= cutoff, cancellationToken);

        var typeStats = await baseQuery
            .Where(a => a.CreatedAt >= cutoff)
            .GroupBy(a => a.AlertType)
            .Select(g => new AlertTrendDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        var severityStats = await baseQuery
            .Where(a => a.CreatedAt >= cutoff)
            .GroupBy(a => a.Severity)
            .Select(g => new AlertTrendDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        return new AlertDashboardStatisticsDto(
            totalActive,
            unacknowledged,
            criticalUnresolved,
            escalated,
            falsePositives,
            typeStats,
            severityStats
        );
    }

    private static IQueryable<Trap_Intel.Domain.Alerts.Alert> ApplySort(
        IQueryable<Trap_Intel.Domain.Alerts.Alert> query,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "createdat" or "created" => descending
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt),

            "updatedat" or "updated" => descending
                ? query.OrderByDescending(a => a.UpdatedAt)
                : query.OrderBy(a => a.UpdatedAt),

            "priority" => descending
                ? query.OrderByDescending(a => a.Priority).ThenByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.Priority).ThenBy(a => a.CreatedAt),

            "severity" => descending
                ? query.OrderByDescending(a => a.Severity).ThenByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.Severity).ThenBy(a => a.CreatedAt),

            "status" => descending
                ? query.OrderByDescending(a => a.Status).ThenByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.Status).ThenBy(a => a.CreatedAt),

            "title" => descending
                ? query.OrderByDescending(a => a.Title).ThenByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt),

            _ => query
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
        };
    }
}
