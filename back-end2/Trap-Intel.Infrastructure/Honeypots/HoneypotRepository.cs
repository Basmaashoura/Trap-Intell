using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Honeypots;

internal sealed class HoneypotRepository : IHoneypotRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HoneypotRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Honeypot honeypot, CancellationToken cancellationToken = default)
    {
        await _dbContext.Honeypots.AddAsync(honeypot, cancellationToken);
    }

    public Task UpdateAsync(Honeypot honeypot, CancellationToken cancellationToken = default)
    {
        _dbContext.Honeypots.Update(honeypot);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid honeypotId, CancellationToken cancellationToken = default)
    {
        var honeypot = await _dbContext.Honeypots
            .FirstOrDefaultAsync(h => h.Id == honeypotId, cancellationToken);

        if (honeypot is null)
        {
            return;
        }

        _dbContext.Honeypots.Remove(honeypot);
    }

    public async Task<Honeypot?> GetByIdAsync(Guid honeypotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .FirstOrDefaultAsync(h => h.Id == honeypotId, cancellationToken);
    }

    public async Task<Honeypot?> GetByExternalServiceIdAsync(string externalServiceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .FirstOrDefaultAsync(h => h.ExternalService != null && h.ExternalService.ServiceId == externalServiceId, cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.OrganizationId == organizationId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.SubscriptionId == subscriptionId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetByStatusAsync(HoneypotStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.Status == status)
            .OrderByDescending(h => h.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetByOrganizationAndStatusAsync(
        Guid organizationId,
        HoneypotStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.OrganizationId == organizationId && h.Status == status)
            .OrderByDescending(h => h.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetByTypeAsync(HoneypotType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.Type == type)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetInactiveAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.Status != HoneypotStatus.Active && h.UpdatedAt <= since)
            .OrderBy(h => h.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetUnhealthyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.Health.Status != HoneypotHealthStatus.Healthy)
            .OrderByDescending(h => h.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetPendingLogFetchAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(interval);

        return await _dbContext.Honeypots
            .Where(h => h.Status == HoneypotStatus.Active && (!h.LastLogFetch.HasValue || h.LastLogFetch <= cutoff))
            .OrderBy(h => h.LastLogFetch)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetExceedingStorageQuotaAsync(decimal maxStorageGb, CancellationToken cancellationToken = default)
    {
        var maxStorageBytes = (long)(maxStorageGb * 1024m * 1024m * 1024m);

        return await _dbContext.Honeypots
            .Where(h => h.Health.StorageUsedBytes > maxStorageBytes)
            .OrderByDescending(h => h.Health.StorageUsedBytes)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .CountAsync(h => h.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<int> CountActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .CountAsync(h => h.OrganizationId == organizationId && h.Status == HoneypotStatus.Active, cancellationToken);
    }

    public async Task<int> CountBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .CountAsync(h => h.SubscriptionId == subscriptionId, cancellationToken);
    }

    public async Task<decimal> GetTotalStorageByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var totalBytes = await _dbContext.Honeypots
            .Where(h => h.OrganizationId == organizationId)
            .SumAsync(h => (long?)h.Health.StorageUsedBytes, cancellationToken) ?? 0L;

        return totalBytes / (1024m * 1024m * 1024m);
    }

    public async Task<(IReadOnlyList<Honeypot> Items, int Total)> GetPagedByOrganizationAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 50 : pageSize;

        var query = _dbContext.Honeypots
            .Where(h => h.OrganizationId == organizationId)
            .OrderByDescending(h => h.CreatedAt);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Honeypot>> GetDeployedWithinRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.DeployedAt.HasValue && h.DeployedAt.Value >= startDate && h.DeployedAt.Value <= endDate)
            .OrderByDescending(h => h.DeployedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Honeypot>> GetByDeploymentLocationAsync(
        HoneypotDeploymentLocation location,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Honeypots
            .Where(h => h.DeploymentLocation == location)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddBatchAsync(IEnumerable<Honeypot> honeypots, CancellationToken cancellationToken = default)
    {
        await _dbContext.Honeypots.AddRangeAsync(honeypots, cancellationToken);
    }

    public Task UpdateBatchAsync(IEnumerable<Honeypot> honeypots, CancellationToken cancellationToken = default)
    {
        _dbContext.Honeypots.UpdateRange(honeypots);
        return Task.CompletedTask;
    }
}
