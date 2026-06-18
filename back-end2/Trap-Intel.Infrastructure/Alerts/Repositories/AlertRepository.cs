using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Alerts.Repositories;

internal sealed class AlertRepository : IAlertRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AlertRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Alert>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetByStatusAsync(Guid organizationId, AlertStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId && a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetBySeverityAsync(Guid organizationId, AlertSeverity minSeverity, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId && a.Severity >= minSeverity && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetByTypeAsync(Guid organizationId, AlertType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId && a.AlertType == type)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetUnacknowledgedAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId && a.Status == AlertStatus.New)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetAssignedToUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.AssignedToUserId == userId && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetRecentAsync(Guid organizationId, int hours = 24, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return await _dbContext.Alerts
            .Where(a => a.OrganizationId == organizationId && a.CreatedAt >= cutoff)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetSnoozedExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // The EF projection needs to evaluate the complex json mapping if it is owned type, 
        // assuming standard structure for expired snoozes.
        return await _dbContext.Alerts
            .Where(a => a.Status == AlertStatus.Snoozed && a.SnoozeInfo != null && a.SnoozeInfo.SnoozeUntil <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Alerts
            .Where(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value <= now && a.Status != AlertStatus.Resolved && a.Status != AlertStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetPendingEscalationAsync(CancellationToken cancellationToken = default)
    {
        // Domain rules dictate escalation happens based on types/timers, simplify rule logic by DB
        var cutoffLevel1 = DateTime.UtcNow.AddHours(-4); // Example SLA mapping
        return await _dbContext.Alerts
            .Where(a => (a.Status == AlertStatus.New || a.Status == AlertStatus.Acknowledged) 
                     && a.CreatedAt <= cutoffLevel1)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Alert>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        // Searches the complex JSONB Source property to identify alerts tied to a specific ID
        return await _dbContext.Alerts
            .Where(a => a.Source != null && a.Source.SourceId == sourceId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _dbContext.Alerts.AddAsync(alert, cancellationToken);
    }

    public Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _dbContext.Alerts.Update(alert);
        return Task.CompletedTask;
    }

    public async Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.CountAsync(a => a.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<int> CountUnacknowledgedAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.CountAsync(a => a.OrganizationId == organizationId && a.Status == AlertStatus.New, cancellationToken);
    }

    public async Task<int> CountBySeverityAsync(Guid organizationId, AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.CountAsync(a => a.OrganizationId == organizationId && a.Severity == severity && a.Status != AlertStatus.Resolved, cancellationToken);
    }
}
