using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for subscription usage tracking and enforcement.
    /// Manages quotas, tracks usage, and enforces limits.
    /// 
    /// Coordinates:
    /// - Subscription (usage statistics)
    /// - Plan (quota limits)
    /// - Usage alerts
    /// </summary>
    public class SubscriptionUsageService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository;

        public SubscriptionUsageService(
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        }

        /// <summary>
        /// Update subscription usage and enforce limits.
        /// 
        /// Workflow:
        /// 1. Get subscription and plan
        /// 2. Update usage statistics
        /// 3. Validate against quota
        /// 4. Calculate percentage usage
        /// 5. Check alert thresholds
        /// 6. Determine enforcement action
        /// 7. Save subscription
        /// 8. Raise usage events
        /// </summary>
        public async Task<Result<UsageValidationResult>> UpdateAndValidateUsageAsync(
            Guid subscriptionId,
            int honeypotsUsed,
            decimal storageUsedGb,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<UsageValidationResult>(
                    Error.Custom("Usage.InvalidSubscription", "Subscription ID cannot be empty."));

            if (honeypotsUsed < 0)
                return Result.Failure<UsageValidationResult>(
                    Error.Custom("Usage.InvalidHoneypots", "Honeypots used cannot be negative."));

            if (storageUsedGb < 0)
                return Result.Failure<UsageValidationResult>(
                    Error.Custom("Usage.InvalidStorage", "Storage used cannot be negative."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<UsageValidationResult>(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Get plan (for quotas)
            var plan = await _planRepository.GetByIdAsync(
                subscription.PlanId, cancellationToken);

            if (plan is null)
                return Result.Failure<UsageValidationResult>(PlanErrors.PlanNotFound);

            // Step 3: Get plan quota
            var quota = GetQuotaForPlan(plan);

            // Step 4: Update usage
            var updateResult = subscription.UpdateUsage(honeypotsUsed, storageUsedGb);
            if (updateResult.IsFailure)
                return Result.Failure<UsageValidationResult>(updateResult.Errors);

            // Step 5: Validate usage
            var validationResult = ValidateUsageAgainstQuota(
                subscription,
                quota,
                honeypotsUsed,
                storageUsedGb);

            // Step 6: Check alerts
            var triggeredAlert = CheckUsageAlerts(validationResult);

            // Step 7: Determine enforcement action
            var action = DetermineEnforcementAction(quota, validationResult, triggeredAlert);

            // Step 8: Save subscription
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            // Create result
            var result = new UsageValidationResult(
                action: action,
                currentPercentage: validationResult.CurrentPercentage,
                remainingQuota: validationResult.RemainingQuota,
                message: GetEnforcementMessage(action, validationResult),
                triggeredAlert: triggeredAlert);

            return Result.Success(result);
        }

        /// <summary>
        /// Get current usage for subscription.
        /// </summary>
        public async Task<Result<CurrentUsage>> GetCurrentUsageAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure<CurrentUsage>(
                    Error.Custom("Usage.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<CurrentUsage>(SubscriptionErrors.SubscriptionNotFound);

            var plan = await _planRepository.GetByIdAsync(
                subscription.PlanId, cancellationToken);

            if (plan is null)
                return Result.Failure<CurrentUsage>(PlanErrors.PlanNotFound);

            var quota = GetQuotaForPlan(plan);

            var usage = new CurrentUsage(
                honeypots: subscription.Usage.HoneypotsUsed,
                storage: subscription.Usage.StorageUsedGb,
                maxHoneypots: quota.MaxHoneypots,
                maxStorage: quota.MaxStorageGb,
                honeypotUsagePercentage: quota.GetHoneypotUsagePercentage(subscription.Usage.HoneypotsUsed),
                storageUsagePercentage: quota.GetStorageUsagePercentage(subscription.Usage.StorageUsedGb),
                remainingHoneypots: quota.GetRemainingHoneypots(subscription.Usage.HoneypotsUsed),
                remainingStorage: quota.GetRemainingStorage(subscription.Usage.StorageUsedGb));

            return Result.Success(usage);
        }

        /// <summary>
        /// Check if usage is within quota.
        /// </summary>
        public async Task<Result<bool>> IsWithinQuotaAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            var usageResult = await GetCurrentUsageAsync(subscriptionId, cancellationToken);
            
            if (usageResult.IsFailure)
                return Result.Failure<bool>(usageResult.Errors);

            var usage = usageResult.Value;
            var withinQuota = usage.HoneypotUsagePercentage <= 100 && usage.StorageUsagePercentage <= 100;

            return Result.Success(withinQuota);
        }

        /// <summary>
        /// Get usage warnings and alerts.
        /// </summary>
        public async Task<Result<List<UsageAlert>>> GetActiveAlertsAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            var usageResult = await GetCurrentUsageAsync(subscriptionId, cancellationToken);
            
            if (usageResult.IsFailure)
                return Result.Failure<List<UsageAlert>>(usageResult.Errors);

            var usage = usageResult.Value;
            var alerts = new List<UsageAlert>();

            // Check storage alert
            if (usage.StorageUsagePercentage >= 80)
            {
                alerts.Add(new UsageAlert(
                    UsageAlertType.StorageWarning,
                    thresholdPercentage: 80,
                    customMessage: $"Storage at {usage.StorageUsagePercentage:F0}% ({usage.Storage:F2}/{usage.MaxStorage} GB)"));
            }

            // Check honeypot alert
            if (usage.HoneypotUsagePercentage >= 80)
            {
                alerts.Add(new UsageAlert(
                    UsageAlertType.HoneypotWarning,
                    thresholdPercentage: 80,
                    customMessage: $"Honeypots at {usage.HoneypotUsagePercentage:F0}% ({usage.Honeypots}/{usage.MaxHoneypots})"));
            }

            // Check exceeded alerts
            if (usage.StorageUsagePercentage > 100)
            {
                alerts.Add(new UsageAlert(
                    UsageAlertType.StorageExceeded,
                    customMessage: $"Storage exceeded by {usage.Storage - usage.MaxStorage:F2} GB"));
            }

            if (usage.HoneypotUsagePercentage > 100)
            {
                alerts.Add(new UsageAlert(
                    UsageAlertType.HoneypotExceeded,
                    customMessage: $"Honeypot limit exceeded by {usage.Honeypots - usage.MaxHoneypots} units"));
            }

            return Result.Success(alerts);
        }

        /// <summary>
        /// Reset monthly usage counters (API calls, etc).
        /// Called at start of each month.
        /// </summary>
        public async Task<Result> ResetMonthlyUsageAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Usage.InvalidSubscription", "Subscription ID cannot be empty."));

            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure(SubscriptionErrors.SubscriptionNotFound);

            // Reset monthly counters (if they exist in usage)
            // This is extensible for future monthly metrics
            
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Get quota for plan type.
        /// </summary>
        private SubscriptionQuota GetQuotaForPlan(Plan plan)
        {
            // Map plan type to quota limits
            // This could be data-driven, but for now it's hardcoded
            return plan.Type switch
            {
                PlanType.Free => new SubscriptionQuota(
                    maxHoneypots: 5,
                    maxStorageGb: 10,
                    maxMonthlyApiCalls: 10000),

                PlanType.Trial => new SubscriptionQuota(
                    maxHoneypots: 10,
                    maxStorageGb: 50,
                    maxMonthlyApiCalls: 50000),

                PlanType.Paid => new SubscriptionQuota(
                    maxHoneypots: 100,
                    maxStorageGb: 500,
                    maxMonthlyApiCalls: 1000000,
                    hardLimitEnforced: true),

                PlanType.Custom => new SubscriptionQuota(
                    maxHoneypots: 1000,
                    maxStorageGb: 5000,
                    maxMonthlyApiCalls: 10000000,
                    hardLimitEnforced: false),

                _ => new SubscriptionQuota(0, 0)
            };
        }

        /// <summary>
        /// Validate usage against quota.
        /// </summary>
        private UsageValidationResult ValidateUsageAgainstQuota(
            Subscription subscription,
            SubscriptionQuota quota,
            int honeypotsUsed,
            decimal storageUsedGb)
        {
            var honeypotPercentage = quota.GetHoneypotUsagePercentage(honeypotsUsed);
            var storagePercentage = quota.GetStorageUsagePercentage(storageUsedGb);
            var avgPercentage = (honeypotPercentage + storagePercentage) / 2;

            var remainingHoneypots = quota.GetRemainingHoneypots(honeypotsUsed);
            var remainingStorage = quota.GetRemainingStorage(storageUsedGb);

            return new UsageValidationResult(
                action: UsageEnforcementAction.Allow,
                currentPercentage: avgPercentage,
                remainingQuota: remainingHoneypots + remainingStorage,
                message: $"Honeypots: {honeypotsUsed}/{quota.MaxHoneypots}, Storage: {storageUsedGb:F2}/{quota.MaxStorageGb} GB");
        }

        /// <summary>
        /// Check if usage triggers any alerts.
        /// </summary>
        private UsageAlert? CheckUsageAlerts(UsageValidationResult validation)
        {
            // Check for warning threshold (80%)
            if (validation.CurrentPercentage >= 80 && validation.CurrentPercentage < 100)
            {
                return new UsageAlert(
                    UsageAlertType.StorageWarning,
                    thresholdPercentage: 80,
                    customMessage: $"Usage at {validation.CurrentPercentage:F0}%");
            }

            // Check for exceeded (>100%)
            if (validation.CurrentPercentage > 100)
            {
                return new UsageAlert(
                    UsageAlertType.StorageExceeded,
                    customMessage: $"Usage exceeded {validation.CurrentPercentage:F0}%");
            }

            return null;
        }

        /// <summary>
        /// Determine what enforcement action to take.
        /// </summary>
        private UsageEnforcementAction DetermineEnforcementAction(
            SubscriptionQuota quota,
            UsageValidationResult validation,
            UsageAlert? triggeredAlert)
        {
            // If exceeded and hard limit enforced, deny
            if (validation.CurrentPercentage > 100 && quota.HardLimitEnforced)
                return UsageEnforcementAction.Denied;

            // If alert triggered, warn
            if (triggeredAlert is not null)
                return UsageEnforcementAction.Warn;

            // Otherwise allow
            return UsageEnforcementAction.Allow;
        }

        /// <summary>
        /// Get human-readable enforcement message.
        /// </summary>
        private string GetEnforcementMessage(
            UsageEnforcementAction action,
            UsageValidationResult validation)
        {
            return action switch
            {
                UsageEnforcementAction.Allow => "Usage is within quota.",
                UsageEnforcementAction.Warn => $"Warning: Usage is at {validation.CurrentPercentage:F0}% of quota.",
                UsageEnforcementAction.RateLimited => "Usage is rate limited due to quota approach.",
                UsageEnforcementAction.Denied => "Operation denied: Usage quota exceeded.",
                _ => "Unknown enforcement status."
            };
        }
    }

    /// <summary>
    /// Current usage snapshot for a subscription.
    /// </summary>
    public record CurrentUsage
    {
        public int Honeypots { get; }
        public decimal Storage { get; }
        public int MaxHoneypots { get; }
        public decimal MaxStorage { get; }
        public decimal HoneypotUsagePercentage { get; }
        public decimal StorageUsagePercentage { get; }
        public int RemainingHoneypots { get; }
        public decimal RemainingStorage { get; }

        public CurrentUsage(
            int honeypots,
            decimal storage,
            int maxHoneypots,
            decimal maxStorage,
            decimal honeypotUsagePercentage,
            decimal storageUsagePercentage,
            int remainingHoneypots,
            decimal remainingStorage)
        {
            Honeypots = honeypots;
            Storage = storage;
            MaxHoneypots = maxHoneypots;
            MaxStorage = maxStorage;
            HoneypotUsagePercentage = honeypotUsagePercentage;
            StorageUsagePercentage = storageUsagePercentage;
            RemainingHoneypots = remainingHoneypots;
            RemainingStorage = remainingStorage;
        }

        public bool IsWithinQuota => HoneypotUsagePercentage <= 100 && StorageUsagePercentage <= 100;
        public bool HasWarning => HoneypotUsagePercentage >= 80 || StorageUsagePercentage >= 80;
        public bool IsExceeded => HoneypotUsagePercentage > 100 || StorageUsagePercentage > 100;
    }
}
