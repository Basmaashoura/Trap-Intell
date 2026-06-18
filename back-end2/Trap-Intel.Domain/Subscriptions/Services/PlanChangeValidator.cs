using System;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for validating subscription plan changes.
    /// 
    /// SINGLE RESPONSIBILITY: Plan change eligibility (upgrades/downgrades).
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (plan change rules)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only plan changes)
    /// 
    /// Lines: ~230 (SOLID-compliant)
    /// </summary>
    public class PlanChangeValidator
    {
        /// <summary>
        /// Check if subscription can be upgraded to a new plan.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active or trial
        /// - New plan must be active
        /// - New plan must have higher customization level
        /// - New plan must not be the same as current plan
        /// </summary>
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
        /// - New plan must have lower customization level
        /// - Current usage must fit within new plan's quota
        /// </summary>
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
        /// Check if subscription can change billing cycle.
        /// 
        /// BUSINESS RULES:
        /// - Subscription must be active
        /// - New billing cycle must be different from current
        /// - Target plan must support the new billing cycle
        /// </summary>
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
    }
}
