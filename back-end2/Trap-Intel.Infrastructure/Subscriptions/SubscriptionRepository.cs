using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Subscriptions;

internal sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SubscriptionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(subscription => subscription.Id == id, cancellationToken);
    }

    public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(subscription => subscription.OrganizationId == organizationId)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(subscription => subscription.Status == status)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetExpiringAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(subscription =>
                subscription.Period.EndDate.HasValue &&
                subscription.Period.EndDate.Value <= expiryThreshold &&
                (subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.Trial))
            .OrderBy(subscription => subscription.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(subscription => subscription.PlanId == planId)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _dbContext.Subscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (subscription is null)
        {
            return;
        }

        _dbContext.Subscriptions.Remove(subscription);
    }

    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .CountAsync(subscription => subscription.Status == SubscriptionStatus.Active, cancellationToken);
    }

    public async Task<int> CountActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .CountAsync(
                subscription => subscription.OrganizationId == organizationId && subscription.Status == SubscriptionStatus.Active,
                cancellationToken);
    }

    public async Task<int> CountByPlanAsync(Guid planId, SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .CountAsync(subscription => subscription.PlanId == planId && subscription.Status == status, cancellationToken);
    }

    private IQueryable<Subscription> QueryWithDetails()
    {
        return _dbContext.Subscriptions
            .AsSplitQuery()
            .Include(subscription => subscription.Quota)
            .Include(subscription => subscription.QuotaHistory)
            .Include(subscription => subscription.UsageSnapshots)
            .Include(subscription => subscription.MonthlySummaries);
    }
}
