using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Repository interface for Honeypot aggregate.
    /// Handles persistence and retrieval of honeypot entities.
    /// </summary>
    public interface IHoneypotRepository
    {
        /// <summary>
        /// Add a new honeypot to the repository.
        /// </summary>
        Task AddAsync(Honeypot honeypot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing honeypot.
        /// </summary>
        Task UpdateAsync(Honeypot honeypot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a honeypot by ID.
        /// </summary>
        Task DeleteAsync(Guid honeypotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypot by ID.
        /// </summary>
        Task<Honeypot?> GetByIdAsync(Guid honeypotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypot by external service ID.
        /// </summary>
        Task<Honeypot?> GetByExternalServiceIdAsync(string externalServiceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all honeypots for an organization.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all honeypots for a subscription.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetBySubscriptionAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots by status.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetByStatusAsync(
            HoneypotStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots by organization and status.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetByOrganizationAndStatusAsync(
            Guid organizationId,
            HoneypotStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots by type.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetByTypeAsync(
            HoneypotType type,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get inactive honeypots (not deployed or terminated).
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetInactiveAsync(
            DateTime since,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots with health issues.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetUnhealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots that need log fetching.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetPendingLogFetchAsync(
            TimeSpan interval,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots exceeding storage quota.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetExceedingStorageQuotaAsync(
            decimal maxStorageGb,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Count honeypots by organization.
        /// </summary>
        Task<int> CountByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Count active honeypots by organization.
        /// </summary>
        Task<int> CountActiveByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Count honeypots by subscription.
        /// </summary>
        Task<int> CountBySubscriptionAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total storage used by organization.
        /// </summary>
        Task<decimal> GetTotalStorageByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get paginated honeypots for organization.
        /// </summary>
        Task<(IReadOnlyList<Honeypot> Items, int Total)> GetPagedByOrganizationAsync(
            Guid organizationId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots deployed within a date range.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetDeployedWithinRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get honeypots by deployment location.
        /// </summary>
        Task<IReadOnlyList<Honeypot>> GetByDeploymentLocationAsync(
            HoneypotDeploymentLocation location,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch add honeypots.
        /// </summary>
        Task AddBatchAsync(IEnumerable<Honeypot> honeypots, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch update honeypots.
        /// </summary>
        Task UpdateBatchAsync(IEnumerable<Honeypot> honeypots, CancellationToken cancellationToken = default);
    }
}
