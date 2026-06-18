using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Domain service that coordinates the creation of a new subscription from a plan.
    /// Handles the complex workflow of validating the plan and creating the subscription.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets Plan aggregate from repository
    /// - Validates plan with business rules
    /// - Creates Subscription aggregate
    /// - Saves subscription to repository
    /// </summary>
    public class CreateSubscriptionService
    {
        private const decimal MaxPersistableStorageGb = 99999999999999.9999m;

        private readonly IPlanRepository _planRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public CreateSubscriptionService(
            IPlanRepository planRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        }

        /// <summary>
        /// Creates a new subscription for an organization on the specified plan.
        /// 
        /// Workflow:
        /// 1. Validates plan exists and is active
        /// 2. Gets pricing for the billing cycle
        /// 3. Creates subscription aggregate with validation
        /// 4. Saves subscription to repository
        /// </summary>
        /// <param name="organizationId">The organization subscribing to the plan</param>
        /// <param name="planId">The plan to subscribe to</param>
        /// <param name="billingCycle">The billing cycle for this subscription</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing the created subscription or error</returns>
        public async Task<Result<Subscription>> CreateAsync(
            Guid organizationId,
            Guid planId,
            BillingCycle billingCycle = BillingCycle.Monthly,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateSubscription.InvalidOrganization", 
                        "Organization ID cannot be empty."));

            if (planId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateSubscription.InvalidPlan", 
                        "Plan ID cannot be empty."));

            // Step 1: Get plan aggregate
            var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is null)
                return Result.Failure<Subscription>(PlanErrors.PlanNotFound);

            if (!plan.IsActive)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateSubscription.PlanInactive",
                        "Cannot create a subscription from an inactive plan."));

            // Step 2: Validate plan with business rule
            var activationRule = new PlanActivationRule(plan);
            if (!activationRule.IsSatisfied())
                return Result.Failure<Subscription>(
                    Error.Custom("CreateSubscription.PlanNotReady", 
                        "Plan is not properly configured for subscriptions."));

            // Step 3: Get pricing for the billing cycle
            var pricing = plan.GetPrice(billingCycle);
            if (pricing is null)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateSubscription.PricingNotFound", 
                        $"No pricing configured for {billingCycle} billing cycle."));

            // Step 4: Create subscription aggregate
            var subscriptionResult = Subscription.Create(
                organizationId,
                planId,
                new SubscriptionPeriod(
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddYears(1),
                    DateTime.UtcNow.AddYears(1)),
                billingCycle,
                new BillingInfo(billingCycle, pricing.Amount));

            if (subscriptionResult.IsFailure)
                return Result.Failure<Subscription>(subscriptionResult.Errors);

            var subscription = subscriptionResult.Value;

            var quotaInitializationResult = InitializeQuotaFromPlan(subscription, plan);
            if (quotaInitializationResult.IsFailure)
            {
                return Result.Failure<Subscription>(quotaInitializationResult.Errors);
            }

            // Step 5: Save subscription to repository
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);

            return Result.Success(subscription);
        }

        /// <summary>
        /// Creates a trial subscription (free, limited time).
        /// Used when organizations are approved.
        /// </summary>
        public async Task<Result<Subscription>> CreateTrialAsync(
            Guid organizationId,
            Guid trialPlanId,
            int trialDays = 14,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.InvalidOrganization", 
                        "Organization ID cannot be empty."));

            if (trialPlanId == Guid.Empty)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.InvalidPlan", 
                        "Trial plan ID cannot be empty."));

            if (trialDays <= 0)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.InvalidTrialDays", 
                        "Trial days must be greater than 0."));

            if (trialDays > 30)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.ExceedsMaxTrialDays",
                        "Trial days cannot exceed 30 days."));

            // Get trial plan
            var trialPlan = await _planRepository.GetByIdAsync(trialPlanId, cancellationToken);
            if (trialPlan is null)
                return Result.Failure<Subscription>(PlanErrors.PlanNotFound);

            if (!trialPlan.IsActive)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.PlanInactive",
                        "Cannot create a trial subscription from an inactive plan."));

            if (trialPlan.Type != PlanType.Trial)
                return Result.Failure<Subscription>(
                    Error.Custom("CreateTrialSubscription.InvalidPlanType",
                        "Trial subscriptions must use a plan of type Trial."));

            // Create trial subscription (free, no pricing needed)
            var now = DateTime.UtcNow;
            var trialEndDate = now.AddDays(trialDays);

            var subscriptionResult = Subscription.Create(
                organizationId,
                trialPlanId,
                new SubscriptionPeriod(now, trialEndDate, null),
                BillingCycle.Monthly,
                new BillingInfo(BillingCycle.Monthly, 0)); // Free trial

            if (subscriptionResult.IsFailure)
                return Result.Failure<Subscription>(subscriptionResult.Errors);

            var subscription = subscriptionResult.Value;

            var quotaInitializationResult = InitializeQuotaFromPlan(subscription, trialPlan);
            if (quotaInitializationResult.IsFailure)
            {
                return Result.Failure<Subscription>(quotaInitializationResult.Errors);
            }

            // Automatically activate trial subscription
            subscription.Activate();

            // Save to repository
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);

            return Result.Success(subscription);
        }

        private static Result InitializeQuotaFromPlan(Subscription subscription, Plan plan)
        {
            var quotaDefinition = plan.QuotaDefinition ?? PlanQuotaDefinition.Unlimited();

            var maxHoneypots = quotaDefinition.MaxHoneypots > 0
                ? quotaDefinition.MaxHoneypots
                : int.MaxValue;

            var maxStorageGb = NormalizeStorageLimit(quotaDefinition.MaxStorageGb);

            var maxMonthlyApiCalls = quotaDefinition.MaxMonthlyApiCalls > 0
                ? quotaDefinition.MaxMonthlyApiCalls
                : int.MaxValue;

            var maxUsers = quotaDefinition.MaxUsers > 0
                ? quotaDefinition.MaxUsers
                : int.MaxValue;

            var overageHoneypotRate = quotaDefinition.OverageHoneypotRate >= 0
                ? quotaDefinition.OverageHoneypotRate
                : 0m;

            var overageStorageRatePerGb = quotaDefinition.OverageStorageRatePerGb >= 0
                ? quotaDefinition.OverageStorageRatePerGb
                : 0m;

            return subscription.InitializeQuota(
                maxHoneypots,
                maxStorageGb,
                maxMonthlyApiCalls,
                maxUsers,
                quotaDefinition.HardLimitEnforced,
                overageHoneypotRate,
                overageStorageRatePerGb);
        }

        private static decimal NormalizeStorageLimit(decimal maxStorageGb)
        {
            if (maxStorageGb <= 0)
            {
                return MaxPersistableStorageGb;
            }

            return Math.Min(maxStorageGb, MaxPersistableStorageGb);
        }
    }
}
