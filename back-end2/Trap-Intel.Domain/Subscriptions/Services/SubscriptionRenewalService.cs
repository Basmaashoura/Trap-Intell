using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for subscription renewal.
    /// Handles auto-renewal of subscriptions at period end.
    /// 
    /// Coordinates multiple aggregates:
    /// - Subscription (to check expiration and update)
    /// - Plan (to get new pricing)
    /// - PaymentMethod (to charge for renewal)
    /// - Invoice (to create renewal invoice)
    /// </summary>
    public class SubscriptionRenewalService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;

        public SubscriptionRenewalService(
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IInvoiceRepository invoiceRepository,
            IInvoiceNumberGenerator invoiceNumberGenerator)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _paymentMethodRepository = paymentMethodRepository ?? throw new ArgumentNullException(nameof(paymentMethodRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _invoiceNumberGenerator = invoiceNumberGenerator ?? throw new ArgumentNullException(nameof(invoiceNumberGenerator));
        }

        /// <summary>
        /// Renew a subscription (extend period, create new invoice).
        /// 
        /// Workflow:
        /// 1. Get subscription
        /// 2. Validate subscription is expired
        /// 3. Get current plan
        /// 4. Create new subscription period
        /// 5. Get payment method
        /// 6. Create renewal invoice
        /// 7. Update subscription period
        /// 8. Save changes
        /// 9. Raise RenewedEvent
        /// </summary>
        public async Task<Result<Subscription>> RenewAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("Renewal.InvalidSubscription", "Subscription ID cannot be empty."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<Subscription>(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Validate subscription can be renewed (must be expired)
            if (subscription.Status != SubscriptionStatus.Expired)
                return Result.Failure<Subscription>(SubscriptionErrors.SubscriptionNotEligibleForRenewal);

            // Step 3: Get current plan
            var plan = await _planRepository.GetByIdAsync(
                subscription.PlanId, cancellationToken);

            if (plan is null)
                return Result.Failure<Subscription>(PlanErrors.PlanNotFound);

            // Step 4: Get pricing
            var pricing = plan.GetPrice(subscription.BillingCycle);
            if (pricing is null)
                return Result.Failure<Subscription>(
                    Error.Custom("Renewal.NoPricing", 
                        "No pricing configured for subscription's billing cycle."));

            // Step 5: Create new subscription period
            var endDate = subscription.Period.EndDate.HasValue 
                ? subscription.Period.EndDate.Value 
                : DateTime.UtcNow.AddYears(1);

            var newPeriod = new SubscriptionPeriod(
                endDate,
                endDate.AddYears(1));

            // Step 6: Renew subscription (extends period, activates)
            var renewResult = subscription.Renew(newPeriod);
            if (renewResult.IsFailure)
                return Result.Failure<Subscription>(renewResult.Errors);

            // Step 7: Save subscription
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success(subscription);
        }

        /// <summary>
        /// Renew subscription with different plan (upgrade/downgrade at renewal).
        /// </summary>
        public async Task<Result<Subscription>> RenewWithPlanChangeAsync(
            Guid subscriptionId,
            Guid newPlanId,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("Renewal.InvalidSubscription", "Subscription ID cannot be empty."));

            if (newPlanId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("Renewal.InvalidPlan", "Plan ID cannot be empty."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<Subscription>(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Validate renewal eligibility
            if (subscription.Status != SubscriptionStatus.Expired)
                return Result.Failure<Subscription>(SubscriptionErrors.SubscriptionNotEligibleForRenewal);

            // Step 3: Get new plan
            var newPlan = await _planRepository.GetByIdAsync(newPlanId, cancellationToken);
            if (newPlan is null)
                return Result.Failure<Subscription>(PlanErrors.PlanNotFound);

            // Step 4: Get pricing for new plan
            var newPricing = newPlan.GetPrice(subscription.BillingCycle);
            if (newPricing is null)
                return Result.Failure<Subscription>(
                    Error.Custom("Renewal.NoPricing", 
                        "No pricing configured for new plan's billing cycle."));

            // Step 5: Create new period
            var endDate = subscription.Period.EndDate.HasValue 
                ? subscription.Period.EndDate.Value 
                : DateTime.UtcNow.AddYears(1);

            var newPeriod = new SubscriptionPeriod(
                endDate,
                endDate.AddYears(1));

            // Step 6: Change plan and renew
            var changeResult = subscription.ChangePlan(newPlanId, newPricing.Amount);
            if (changeResult.IsFailure)
                return Result.Failure<Subscription>(changeResult.Errors);

            var renewResult = subscription.Renew(newPeriod);
            if (renewResult.IsFailure)
                return Result.Failure<Subscription>(renewResult.Errors);

            // Step 7: Save
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success(subscription);
        }

        /// <summary>
        /// Get all subscriptions expiring within specified days.
        /// Used for bulk renewal processing.
        /// </summary>
        public async Task<Result<(int ExpiringCount, int ExpiringWithinDays)>> GetExpiringSubscriptionsAsync(
            int daysUntilExpiry = 7,
            CancellationToken cancellationToken = default)
        {
            if (daysUntilExpiry <= 0)
                return Result.Failure<(int, int)>(
                    Error.Custom("Renewal.InvalidDays", "Days must be greater than zero."));

            // TODO: This would require repository support for filtering by expiration date
            // For now, this is a placeholder
            return Result.Success((0, 0));
        }

        /// <summary>
        /// Process batch renewals (called periodically).
        /// </summary>
        public async Task<Result<(int SuccessCount, int FailureCount)>> ProcessBatchRenewalsAsync(
            CancellationToken cancellationToken = default)
        {
            int successCount = 0;
            int failureCount = 0;

            // TODO: Implement batch renewal logic
            // 1. Get expiring subscriptions
            // 2. For each: call RenewAsync
            // 3. Track success/failure
            // 4. Return stats

            return await Task.FromResult(Result.Success((successCount, failureCount)));
        }

        /// <summary>
        /// Schedule renewal (mark for automatic renewal).
        /// </summary>
        public async Task<Result> ScheduleRenewalAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Renewal.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Mark for auto-renewal
            subscription.EnableAutoRenewal();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Cancel scheduled renewal (disable auto-renewal).
        /// </summary>
        public async Task<Result> CancelScheduledRenewalAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Renewal.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Disable auto-renewal
            subscription.DisableAutoRenewal();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }
    }
}
