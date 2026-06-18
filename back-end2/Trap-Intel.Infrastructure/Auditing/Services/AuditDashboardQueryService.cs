using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Auditing.Services;

internal sealed class AuditDashboardQueryService : IAuditDashboardQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditDashboardQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditDashboardStatisticsDto> GetDashboardStatisticsAsync(Guid organizationId, int lastNDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-lastNDays);

        // We use IQueryable projections (Select) to avoid loading entire entity objects into memory
        var baseQuery = _dbContext.AuditTrails
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.Timestamp >= cutoffDate);

        var totalEvents = await baseQuery.CountAsync(cancellationToken);

        var unacknowledgedCritical = await baseQuery
            .CountAsync(a => a.Severity == AuditSeverity.Critical && !a.IsAcknowledged, cancellationToken);

        var highSeverityEvents = await baseQuery
            .CountAsync(a => a.Severity == AuditSeverity.Warning || a.Severity == AuditSeverity.Critical, cancellationToken);

        var topResourceTypesQuery = await baseQuery
            .GroupBy(a => a.ResourceType)
            .Select(g => new { ResourceType = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentCriticalEventsQuery = await baseQuery
            .Where(a => a.Severity == AuditSeverity.Critical && !a.IsAcknowledged)
            .OrderByDescending(a => a.Timestamp)
            .Take(5)
            .Select(a => new RecentEventDto(a.Id, a.Action, a.ResourceType, a.Timestamp, a.Reason))
            .ToListAsync(cancellationToken);

        return new AuditDashboardStatisticsDto
        {
            TotalEvents = totalEvents,
            UnacknowledgedCriticalEvents = unacknowledgedCritical,
            HighSeverityEvents = highSeverityEvents,
            TopResourceTypes = topResourceTypesQuery.Select(r => new EventsByResourceDto(r.ResourceType, r.Count)).ToList(),
            RecentCriticalEvents = recentCriticalEventsQuery
        };
    }
}
