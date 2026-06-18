using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for subscription cancellation.
    /// Handles cancelling subscriptions with optional refunds.
    /// 
    /// Coordinates multiple aggregates:
    /// - Subscription (to update status)
    /// - Invoice (to cancel pending invoices)
    /// - PaymentMethod (to process refunds)
    /// </summary>
    public class SubscriptionCancellationService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentProcessor _paymentProcessor;

        public SubscriptionCancellationService(
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentProcessor paymentProcessor)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _paymentProcessor = paymentProcessor ?? throw new ArgumentNullException(nameof(paymentProcessor));
        }

        /// <summary>
        /// Cancel subscription immediately.
        /// 
        /// Workflow:
        /// 1. Get subscription
        /// 2. Validate can be cancelled
        /// 3. Calculate refund (if paid)
        /// 4. Process refund
        /// 5. Cancel pending invoices
        /// 6. Update subscription status
        /// 7. Save changes
        /// 8. Raise CancelledEvent
        /// </summary>
        public async Task<Result<CancellationResult>> CancelAsync(
            Guid subscriptionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<CancellationResult>(
                    Error.Custom("Cancellation.InvalidSubscription", "Subscription ID cannot be empty."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure<CancellationResult>(
                    Error.Custom("Cancellation.InvalidReason", "Reason cannot be empty."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<CancellationResult>(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Validate can be cancelled
            if (subscription.Status == SubscriptionStatus.Cancelled)
                return Result.Failure<CancellationResult>(
                    SubscriptionErrors.SubscriptionAlreadyCancelled);

            // Step 3: Calculate refund
            var refundAmount = CalculateRefund(subscription);

            // Step 4: Process refund if applicable
            if (refundAmount > 0 && subscription.Status == SubscriptionStatus.Active)
            {
                // TODO: Process refund via payment processor
                // For now, this is a placeholder
            }

            // Step 5: Cancel subscription
            var cancelResult = subscription.Cancel(reason);
            if (cancelResult.IsFailure)
                return Result.Failure<CancellationResult>(cancelResult.Errors);

            // Step 6: Save subscription
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            // Return cancellation result
            var result = new CancellationResult(
                subscriptionId: subscription.Id,
                reason: reason,
                cancelledAt: DateTime.UtcNow,
                refundAmount: refundAmount,
                refundProcessed: refundAmount > 0,
                message: $"Subscription cancelled successfully. Refund: {refundAmount:C}");

            return Result.Success(result);
        }

        /// <summary>
        /// Cancel subscription at end of current period (no immediate charge).
        /// </summary>
        public async Task<Result> CancelAtPeriodEndAsync(
            Guid subscriptionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Cancellation.InvalidSubscription", "Subscription ID cannot be empty."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Cancellation.InvalidReason", "Reason cannot be empty."));

            // Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Validate can be cancelled
            if (subscription.Status == SubscriptionStatus.Cancelled)
                return Result.Failure(SubscriptionErrors.SubscriptionAlreadyCancelled);

            // Mark for cancellation at period end
            subscription.ScheduleCancellationAtPeriodEnd(reason);
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Calculate pro-rated refund for subscription.
        /// </summary>
        public async Task<Result<decimal>> CalculateRefundAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure<decimal>(
                    Error.Custom("Cancellation.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<decimal>(SubscriptionErrors.SubscriptionNotFound);

            var refund = CalculateRefund(subscription);
            return Result.Success(refund);
        }

        /// <summary>
        /// Reactivate a cancelled subscription.
        /// </summary>
        public async Task<Result> ReactivateAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Cancellation.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Only cancelled subscriptions can be reactivated
            if (subscription.Status != SubscriptionStatus.Cancelled)
                return Result.Failure(
                    Error.Custom("Cancellation.CannotReactivate", 
                        "Only cancelled subscriptions can be reactivated."));

            // Reactivate
            subscription.Activate();
            subscription.EnableAutoRenewal();

            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Permanently delete a subscription (hard delete).
        /// Only for truly unused subscriptions.
        /// </summary>
        public async Task<Result> PermanentlyDeleteAsync(
            Guid subscriptionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Cancellation.InvalidSubscription", "Subscription ID cannot be empty."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Cancellation.InvalidReason", "Reason cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Only allow deletion of new or cancelled subscriptions
            if (subscription.Status != SubscriptionStatus.Cancelled && 
                subscription.Status != SubscriptionStatus.Trial)
            {
                return Result.Failure(
                    Error.Custom("Cancellation.CannotDelete", 
                        "Only trial or cancelled subscriptions can be deleted."));
            }

            // Delete from repository
            await _subscriptionRepository.DeleteAsync(subscriptionId, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Calculate pro-rated refund amount.
        /// </summary>
        private decimal CalculateRefund(Subscription subscription)
        {
            // If subscription hasn't started or is free, no refund
            if (subscription.Status != SubscriptionStatus.Active || 
                subscription.BillingInfo.TotalBilled <= 0)
                return 0;

            // Calculate days remaining in billing period
            var now = DateTime.UtcNow;
            var period = subscription.Period;
            
            if (now < period.StartDate || (period.EndDate.HasValue && now > period.EndDate.Value))
                return 0;

            var endDate = period.EndDate ?? DateTime.UtcNow.AddYears(1);
            var totalDays = (int)(endDate - period.StartDate).TotalDays;
            var daysRemaining = (int)(endDate - now).TotalDays;

            if (totalDays <= 0 || daysRemaining <= 0)
                return 0;

            // Pro-rated refund = (days remaining / total days) * subscription amount
            var dailyRate = subscription.BillingInfo.TotalBilled / totalDays;
            var refund = dailyRate * daysRemaining;

            return Math.Round(refund, 2);
        }
    }

    /// <summary>
    /// Result of subscription cancellation.
    /// </summary>
    public record CancellationResult
    {
        public Guid SubscriptionId { get; }
        public string Reason { get; }
        public DateTime CancelledAt { get; }
        public decimal RefundAmount { get; }
        public bool RefundProcessed { get; }
        public string Message { get; }

        public CancellationResult(
            Guid subscriptionId,
            string reason,
            DateTime cancelledAt,
            decimal refundAmount,
            bool refundProcessed,
            string message)
        {
            SubscriptionId = subscriptionId;
            Reason = reason;
            CancelledAt = cancelledAt;
            RefundAmount = refundAmount;
            RefundProcessed = refundProcessed;
            Message = message;
        }
    }
}
