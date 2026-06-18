using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Repository interface for PaymentMethod aggregates.
    /// Data access abstraction for persistence.
    /// </summary>
    public interface IPaymentMethodRepository
    {
        /// <summary>
        /// Get payment method by ID.
        /// </summary>
        Task<PaymentMethod?> GetByIdAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all payment methods for an organization.
        /// </summary>
        Task<IEnumerable<PaymentMethod>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get default payment method for organization.
        /// </summary>
        Task<PaymentMethod?> GetDefaultByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get active payment methods for organization.
        /// </summary>
        Task<IEnumerable<PaymentMethod>> GetActiveByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payment methods by status.
        /// </summary>
        Task<IEnumerable<PaymentMethod>> GetByStatusAsync(
            PaymentMethodStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get expired payment methods.
        /// </summary>
        Task<IEnumerable<PaymentMethod>> GetExpiredAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new payment method.
        /// </summary>
        Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing payment method.
        /// </summary>
        Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete payment method.
        /// </summary>
        Task DeleteAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Count payment methods for organization.
        /// </summary>
        Task<int> CountByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}
