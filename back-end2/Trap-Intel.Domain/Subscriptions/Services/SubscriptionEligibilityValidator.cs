using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for validating subscription operation eligibility.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (eligibility validation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain objects only
    /// ? Encapsulates domain knowledge about subscription rules
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic validation)
    /// - Single Responsibility (only eligibility validation)
    /// - Domain-driven (uses Subscription, Plan domain objects)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In SubscriptionPlanChangeRule:
    ///   public async Task<bool> IsSatisfiedAsync(...)
    ///   {
    ///       if (_subscription.Status == SubscriptionStatus.Cancelled)
    ///           return false;
    ///       // ?? Logic scattered across business rules. Should be centralized!
    ///   }
    /// 
    /// This service centralizes all subscription eligibility logic.
    /// </summary>
    public class SubscriptionEligibilityValidator
    {
        /// <summary>
        /// Check if subscription can be upgraded to a new plan.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active or trial
        /// - New plan must be active
        /// - New plan must have higher customization level (true upgrade)
        /// - New plan must not be the same as current plan
        /// 
        /// EXAMPLE:
        /// - Current: Basic plan
        /// - Target: Professional plan
        /// - Result: ? Can upgrade
        /// </summary>
        /// <param name="subscription">Current subscription</param>
        /// <param name="currentPlan">Current plan details</param>
        /// <param name="targetPlan">Target plan to upgrade to</param>
        /// <returns>Eligibility result with reason if not allowed</returns>
        public EligibilityResult CanUpgradePlan(
            Subscription subscription,
            Plan currentPlan,
            Plan targetPlan)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            if (currentPlan == null)
                throw new ArgumentNullException(nameof(currentPlan));

            if (targetPlan == null)
                throw new ArgumentNullException(nameof(targetPlan));

            // Rule 1: Subscription must be active or trial
            if (subscription.Status != SubscriptionStatus.Active &&
                subscription.Status != SubscriptionStatus.Trial)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot upgrade. Subscription status is {subscription.Status}. " +
                    $"Only Active or Trial subscriptions can be upgraded.");
            }

            // Rule 2: Target plan must be active
            if (!targetPlan.IsActive)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot upgrade to inactive plan.");
            }

            // Rule 3: Cannot "upgrade" to same plan
            if (subscription.PlanId == targetPlan.Id)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot upgrade to the same plan.");
            }

            // Rule 4: Target must have higher customization level
            if (targetPlan.CustomizationLevel <= currentPlan.CustomizationLevel)
            {
                return EligibilityResult.NotAllowed(
                    $"Target plan ({targetPlan.CustomizationLevel}) must have higher " +
                    $"customization level than current plan ({currentPlan.CustomizationLevel}). " +
                    $"Use downgrade instead.");
            }

            return EligibilityResult.Allowed(
                $"Can upgrade from {currentPlan.Name} to {targetPlan.Name}.");
        }

        /// <summary>
        /// Check if subscription can be downgraded to a new plan.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active
        /// - New plan must be active
        /// - New plan must have lower customization level (true downgrade)
        /// - Current usage must fit within new plan's quota
        /// 
        /// EXAMPLE:
        /// - Current: Professional (100 honeypots quota)
        /// - Target: Basic (10 honeypots quota)
        /// - Current usage: 15 honeypots
        /// - Result: ? Cannot downgrade (usage exceeds new quota)
        /// </summary>
        /// <param name="subscription">Current subscription</param>
        /// <param name="currentPlan">Current plan details</param>
        /// <param name="targetPlan">Target plan to downgrade to</param>
        /// <param name="currentUsage">Current usage statistics</param>
        /// <param name="targetQuota">Target plan's quota</param>
        /// <returns>Eligibility result with reason</returns>
        public EligibilityResult CanDowngradePlan(
            Subscription subscription,
            Plan currentPlan,
            Plan targetPlan,
            UsageStatistics currentUsage,
            SubscriptionQuota targetQuota)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            if (currentPlan == null)
                throw new ArgumentNullException(nameof(currentPlan));

            if (targetPlan == null)
                throw new ArgumentNullException(nameof(targetPlan));

            if (currentUsage == null)
                throw new ArgumentNullException(nameof(currentUsage));

            if (targetQuota == null)
                throw new ArgumentNullException(nameof(targetQuota));

            // Rule 1: Subscription must be active
            if (subscription.Status != SubscriptionStatus.Active)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot downgrade. Subscription status is {subscription.Status}. " +
                    $"Only Active subscriptions can be downgraded.");
            }

            // Rule 2: Target plan must be active
            if (!targetPlan.IsActive)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot downgrade to inactive plan.");
            }

            // Rule 3: Cannot "downgrade" to same plan
            if (subscription.PlanId == targetPlan.Id)
            {
                return EligibilityResult.NotAllowed(
                    "Cannot downgrade to the same plan.");
            }

            // Rule 4: Target must have lower customization level
            if (targetPlan.CustomizationLevel >= currentPlan.CustomizationLevel)
            {
                return EligibilityResult.NotAllowed(
                    $"Target plan ({targetPlan.CustomizationLevel}) must have lower " +
                    $"customization level than current plan ({currentPlan.CustomizationLevel}). " +
                    $"Use upgrade instead.");
            }

            // Rule 5: Current usage must fit in new quota
            if (currentUsage.HoneypotsUsed > targetQuota.MaxHoneypots)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot downgrade. Current honeypot usage ({currentUsage.HoneypotsUsed}) " +
                    $"exceeds target plan's quota ({targetQuota.MaxHoneypots}). " +
                    $"Reduce usage to {targetQuota.MaxHoneypots} or fewer honeypots first.");
            }

            if (currentUsage.StorageUsedGb > targetQuota.MaxStorageGb)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot downgrade. Current storage usage ({currentUsage.StorageUsedGb:F2}GB) " +
                    $"exceeds target plan's quota ({targetQuota.MaxStorageGb}GB). " +
                    $"Reduce storage to {targetQuota.MaxStorageGb}GB or less first.");
            }

            return EligibilityResult.Allowed(
                $"Can downgrade from {currentPlan.Name} to {targetPlan.Name}. " +
                $"Usage fits within new quota.");
        }

        /// <summary>
        /// Check if subscription can be renewed.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be expired or trial
        /// - Subscription must not be cancelled
        /// - Subscription must not have scheduled cancellation
        /// - Auto-renew can be on or off (manual vs automatic renewal)
        /// 
        /// EXAMPLE:
        /// - Status: Expired
        /// - Cancelled: No
        /// - Result: ? Can renew
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <returns>Eligibility result</returns>
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
        /// - Can cancel at any time (immediate or end-of-period)
        /// 
        /// EXAMPLE:
        /// - Status: Active
        /// - Result: ? Can cancel
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <returns>Eligibility result</returns>
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
        /// - Usually due to payment failure or policy violation
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <returns>Eligibility result</returns>
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
        /// - Payment issues must be resolved (if applicable)
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <returns>Eligibility result</returns>
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
        /// Check if subscription can change billing cycle.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active
        /// - New billing cycle must be different from current
        /// - Target plan must support the new billing cycle
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <param name="newBillingCycle">New billing cycle</param>
        /// <param name="plan">Current plan</param>
        /// <returns>Eligibility result</returns>
        public EligibilityResult CanChangeBillingCycle(
            Subscription subscription,
            BillingCycle newBillingCycle,
            Plan plan)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            // Rule 1: Subscription must be active
            if (subscription.Status != SubscriptionStatus.Active)
            {
                return EligibilityResult.NotAllowed(
                    $"Cannot change billing cycle. Subscription status is {subscription.Status}. " +
                    $"Only Active subscriptions can change billing cycle.");
            }

            // Rule 2: New cycle must be different
            if (subscription.BillingCycle == newBillingCycle)
            {
                return EligibilityResult.NotAllowed(
                    $"Subscription is already on {newBillingCycle} billing cycle.");
            }

            // Rule 3: Plan must support new billing cycle
            var pricing = plan.GetPrice(newBillingCycle);
            if (pricing == null)
            {
                return EligibilityResult.NotAllowed(
                    $"Plan {plan.Name} does not support {newBillingCycle} billing cycle.");
            }

            return EligibilityResult.Allowed(
                $"Can change from {subscription.BillingCycle} to {newBillingCycle}.");
        }

        /// <summary>
        /// Validate multiple operations at once for comprehensive check.
        /// Useful for UI to show what operations are available.
        /// </summary>
        /// <param name="subscription">Subscription to check</param>
        /// <param name="currentPlan">Current plan</param>
        /// <param name="targetPlan">Target plan (optional)</param>
        /// <param name="currentUsage">Current usage (optional)</param>
        /// <param name="targetQuota">Target quota (optional)</param>
        /// <returns>Dictionary of operation eligibility</returns>
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

            // Basic operations (don't need additional info)
            results[SubscriptionOperation.Renew] = CanRenew(subscription);
            results[SubscriptionOperation.Cancel] = CanCancel(subscription);
            results[SubscriptionOperation.Suspend] = CanSuspend(subscription);
            results[SubscriptionOperation.Reactivate] = CanReactivate(subscription);

            // Operations requiring target plan
            if (targetPlan != null)
            {
                results[SubscriptionOperation.Upgrade] = 
                    CanUpgradePlan(subscription, currentPlan, targetPlan);

                if (currentUsage != null && targetQuota != null)
                {
                    results[SubscriptionOperation.Downgrade] = 
                        CanDowngradePlan(subscription, currentPlan, targetPlan, currentUsage, targetQuota);
                }
            }

            return results;
        }
    }

    #region Supporting Types

    /// <summary>
    /// Result of eligibility validation.
    /// </summary>
    public record EligibilityResult
    {
        public bool IsAllowed { get; init; }
        public string Reason { get; init; }

        private EligibilityResult(bool isAllowed, string reason)
        {
            IsAllowed = isAllowed;
            Reason = reason ?? string.Empty;
        }

        public static EligibilityResult Allowed(string reason) =>
            new(true, reason);

        public static EligibilityResult NotAllowed(string reason) =>
            new(false, reason);
    }

    /// <summary>
    /// Types of subscription operations.
    /// </summary>
    public enum SubscriptionOperation
    {
        Upgrade,
        Downgrade,
        Renew,
        Cancel,
        Suspend,
        Reactivate,
        ChangeBillingCycle
    }

    #endregion
}
