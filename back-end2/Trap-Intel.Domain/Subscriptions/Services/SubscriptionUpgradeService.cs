using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Domain service that coordinates subscription plan upgrades/downgrades.
    /// Handles the complex workflow of changing a subscription's plan with validation.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets Subscription aggregate from repository
    /// - Gets both current and new Plan aggregates
    /// - Validates the plan change with business rules
    /// - Updates the subscription with new plan
    /// - Saves changes to repository
    /// </summary>
    public class SubscriptionUpgradeService
    {
        private readonly IPlanRepository _planRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionUpgradeService(
            IPlanRepository planRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        }

        /// <summary>
        /// Upgrades or downgrades a subscription to a different plan.
        /// 
        /// Workflow:
        /// 1. Gets subscription from repository
        /// 2. Validates subscription is in valid state
        /// 3. Gets current and new plan aggregates
        /// 4. Validates plan change is allowed with business rules
        /// 5. Gets pricing for the new plan
        /// 6. Updates subscription with new plan
        /// 7. Saves changes
        /// </summary>
        /// <param name="subscriptionId">The subscription to upgrade/downgrade</param>
        /// <param name="newPlanId">The new plan ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success or failure</returns>
        public async Task<Result> ChangeAsync(
            Guid subscriptionId,
            Guid newPlanId,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionChange.InvalidId", 
                        "Subscription ID cannot be empty."));

            if (newPlanId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionChange.InvalidNewPlan", 
                        "New plan ID cannot be empty."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Validate subscription is in valid state for plan change
            if (subscription.Status == SubscriptionStatus.Cancelled)
                return Result.Failure(
                    Error.Custom("SubscriptionChange.Cancelled", 
                        "Cannot change plan for cancelled subscription."));

            if (subscription.Status == SubscriptionStatus.Expired)
                return Result.Failure(
                    Error.Custom("SubscriptionChange.Expired", 
                        "Cannot change plan for expired subscription."));

            // Step 3: Get current plan
            var currentPlan = await _planRepository.GetByIdAsync(
                subscription.PlanId, cancellationToken);

            if (currentPlan is null)
                return Result.Failure(PlanErrors.PlanNotFound);

            // Step 4: Get new plan
            var newPlan = await _planRepository.GetByIdAsync(
                newPlanId, cancellationToken);

            if (newPlan is null)
                return Result.Failure(PlanErrors.PlanNotFound);

            // Step 5: Validate plan change with business rule
            var planChangeRule = new SubscriptionPlanChangeRule(
                subscription, newPlan, _planRepository);

            if (!await planChangeRule.IsSatisfiedAsync(cancellationToken))
                return Result.Failure(planChangeRule.Error);

            // Step 6: Get pricing for the new plan
            var newPrice = newPlan.GetPrice(subscription.BillingCycle);
            if (newPrice is null)
                return Result.Failure(
                    Error.Custom("SubscriptionChange.NoPricing", 
                        $"No pricing available for {subscription.BillingCycle} billing cycle in new plan."));

            // Step 7: Calculate proration (simple implementation)
            var prorationAmount = CalculateProration(
                currentPlan.GetPrice(subscription.BillingCycle),
                newPrice);

            // Step 8: Update subscription with new plan
            subscription.ChangePlan(newPlanId, newPrice.Amount);

            // Step 9: Save changes
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Performs a simple upgrade (to higher tier) with validation.
        /// </summary>
        public async Task<Result> UpgradeAsync(
            Guid subscriptionId,
            Guid newPlanId,
            CancellationToken cancellationToken = default)
        {
            // Validation - could add additional checks for actual upgrade
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionUpgrade.InvalidId", 
                        "Subscription ID cannot be empty."));

            if (newPlanId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionUpgrade.InvalidNewPlan", 
                        "New plan ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            var newPlan = await _planRepository.GetByIdAsync(
                newPlanId, cancellationToken);

            if (newPlan is null)
                return Result.Failure(PlanErrors.PlanNotFound);

            // Proceed with change
            return await ChangeAsync(subscriptionId, newPlanId, cancellationToken);
        }

        /// <summary>
        /// Performs a simple downgrade (to lower tier) with validation.
        /// </summary>
        public async Task<Result> DowngradeAsync(
            Guid subscriptionId,
            Guid newPlanId,
            CancellationToken cancellationToken = default)
        {
            // Validation - could add additional checks for actual downgrade
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionDowngrade.InvalidId", 
                        "Subscription ID cannot be empty."));

            if (newPlanId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("SubscriptionDowngrade.InvalidNewPlan", 
                        "New plan ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            var newPlan = await _planRepository.GetByIdAsync(
                newPlanId, cancellationToken);

            if (newPlan is null)
                return Result.Failure(PlanErrors.PlanNotFound);

            // Check if downgrade is allowed (usage fits in new plan)
            var downgradeRule = new SubscriptionPlanChangeRule(
                subscription, newPlan, _planRepository);

            if (!await downgradeRule.IsSatisfiedAsync(cancellationToken))
                return Result.Failure(downgradeRule.Error);

            // Proceed with change
            return await ChangeAsync(subscriptionId, newPlanId, cancellationToken);
        }

        /// <summary>
        /// Calculates proration amount for plan change.
        /// This is a simplified implementation. Real implementation would be more complex.
        /// </summary>
        private decimal CalculateProration(PlanPrice? currentPrice, PlanPrice? newPrice)
        {
            if (currentPrice is null || newPrice is null)
                return 0;

            // Simple calculation: difference in monthly price
            var monthlyDifference = newPrice.Amount - currentPrice.Amount;

            // Would need to calculate based on days remaining in billing cycle
            // This is simplified for demonstration
            return monthlyDifference;
        }
    }
}
