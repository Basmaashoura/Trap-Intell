using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for validating subscription lifecycle operations.
    /// 
    /// SINGLE RESPONSIBILITY: Lifecycle operation eligibility (renew/cancel/suspend).
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (lifecycle rules)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only lifecycle operations)
    /// 
    /// Lines: ~180 (SOLID-compliant)
    /// </summary>
    public class SubscriptionLifecycleValidator
    {
        /// <summary>
        /// Check if subscription can be renewed.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be expired or trial
        /// - Subscription must not be cancelled
        /// - Subscription must not have scheduled cancellation
        /// </summary>
        public EligibilityResult CanRenew(Subscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            // Rule 1: Cannot renew cancelled subscriptions
            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot renew cancelled subscription. Create new subscription instead.");
            }

            // Rule 2: Cannot renew if cancellation is scheduled
            if (subscription.IsCancellationScheduled)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot renew subscription with scheduled cancellation. " +
                    "Remove cancellation first.");
            }

            // Rule 3: Must be expired or trial to manually renew
            if (subscription.Status != SubscriptionStatus.Expired &&
                subscription.Status != SubscriptionStatus.Trial)
            {
                return EligibilityResult.NotAllowed(
                    $"Subscription is {subscription.Status}. " +
                    $"Only Expired or Trial subscriptions can be manually renewed.");
            }

            return EligibilityResult.Allowed(
                "Subscription can be renewed.");
        }

        /// <summary>
        /// Check if subscription can be cancelled.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active or trial
        /// - Subscription must not already be cancelled
        /// </summary>
        public EligibilityResult CanCancel(Subscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            // Rule 1: Cannot cancel if already cancelled
            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return EligibilityResult.NotAllowed(
                    "Subscription is already cancelled.");
            }

            // Rule 2: Can only cancel active or trial subscriptions
            if (subscription.Status != SubscriptionStatus.Active &&
                subscription.Status != SubscriptionStatus.Trial)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot cancel subscription with status {subscription.Status}. " +
                    $"Only Active or Trial subscriptions can be cancelled.");
            }

            return EligibilityResult.Allowed(
                "Subscription can be cancelled.");
        }

        /// <summary>
        /// Check if subscription can be suspended (admin action).
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active
        /// - Subscription must not already be suspended
        /// </summary>
        public EligibilityResult CanSuspend(Subscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            // Rule 1: Cannot suspend if already suspended
            if (subscription.Status == SubscriptionStatus.Suspended)
            {
                return EligibilityResult.NotAllowed(
                    "Subscription is already suspended.");
            }

            // Rule 2: Can only suspend active subscriptions
            if (subscription.Status != SubscriptionStatus.Active)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot suspend subscription with status {subscription.Status}. " +
                    $"Only Active subscriptions can be suspended.");
            }

            return EligibilityResult.Allowed(
                "Subscription can be suspended.");
        }

        /// <summary>
        /// Check if subscription can be reactivated.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be suspended
        /// </summary>
        public EligibilityResult CanReactivate(Subscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            // Rule 1: Can only reactivate suspended subscriptions
            if (subscription.Status != SubscriptionStatus.Suspended)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot reactivate subscription with status {subscription.Status}. " +
                    $"Only Suspended subscriptions can be reactivated.");
            }

            return EligibilityResult.Allowed(
                "Subscription can be reactivated.");
        }

        /// <summary>
        /// Validate multiple operations at once for comprehensive check.
        /// Useful for UI to show what operations are available.
        /// </summary>
        public Dictionary<SubscriptionOperation, EligibilityResult> ValidateAllOperations(
            Subscription subscription,
            Plan currentPlan,
            Plan? targetPlan = null,
            UsageStatistics? currentUsage = null,
            SubscriptionQuota? targetQuota = null)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            if (currentPlan == null)
                throw new ArgumentNullException(nameof(currentPlan));

            var results = new Dictionary<SubscriptionOperation, EligibilityResult>();

            // Basic lifecycle operations
            results[SubscriptionOperation.Renew] = CanRenew(subscription);
            results[SubscriptionOperation.Cancel] = CanCancel(subscription);
            results[SubscriptionOperation.Suspend] = CanSuspend(subscription);
            results[SubscriptionOperation.Reactivate] = CanReactivate(subscription);

            return results;
        }
    }
}
