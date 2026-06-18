using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Repository interface for Subscription aggregate.
    /// </summary>
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Subscription>> GetExpiringAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default);
        Task<IEnumerable<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
        Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
        Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);
        Task<int> CountActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<int> CountByPlanAsync(Guid planId, SubscriptionStatus status, CancellationToken cancellationToken = default);
    }
}
