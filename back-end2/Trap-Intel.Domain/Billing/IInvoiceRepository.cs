using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Repository interface for Invoice aggregates.
    /// Data access abstraction for persistence.
    /// </summary>
    public interface IInvoiceRepository
    {
        /// <summary>
        /// Get invoice by ID.
        /// </summary>
        Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get invoice by invoice number.
        /// </summary>
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get invoice by linked payment ID.
        /// </summary>
        Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all invoices for a subscription.
        /// </summary>
        Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all invoices for an organization.
        /// </summary>
        Task<IEnumerable<Invoice>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get invoices by status.
        /// </summary>
        Task<IEnumerable<Invoice>> GetByStatusAsync(
            InvoiceStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get overdue invoices.
        /// </summary>
        Task<IEnumerable<Invoice>> GetOverdueAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new invoice.
        /// </summary>
        Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing invoice.
        /// </summary>
        Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete invoice.
        /// </summary>
        Task DeleteAsync(Guid invoiceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if invoice number exists.
        /// </summary>
        Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check whether an invoice exists for the given subscription and billing period.
        /// </summary>
        Task<bool> ExistsForSubscriptionPeriodAsync(
            Guid subscriptionId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken = default);
    }
}
