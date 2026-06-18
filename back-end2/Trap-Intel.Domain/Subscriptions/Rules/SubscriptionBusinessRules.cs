using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Enforces subscription renewal requirements.
    /// Ensures subscription can be safely renewed.
    /// </summary>
    public class SubscriptionRenewalRule : IAsyncBusinessRule
    {
        private readonly Subscription _subscription;
        private readonly IPlanRepository _planRepository;
        private readonly IPaymentMethodRepository? _paymentMethodRepository;

        public SubscriptionRenewalRule(
            Subscription subscription,
            IPlanRepository planRepository,
            IPaymentMethodRepository? paymentMethodRepository = null)
        {
            _subscription = subscription;
            _planRepository = planRepository;
            _paymentMethodRepository = paymentMethodRepository;
        }

        public Error Error => SubscriptionErrors.CannotRenewCancelledSubscription;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Rule 1: Can only renew expired subscriptions
            if (_subscription.Status != SubscriptionStatus.Expired)
                return false;

            // Rule 2: Cannot renew cancelled subscriptions
            if (_subscription.CancellationInfo is not null)
                return false;

            // Rule 3: Plan must still be active
            var plan = await _planRepository
                .GetByIdAsync(_subscription.PlanId, cancellationToken);

            if (plan is null || !plan.IsActive)
                return false;

            // Rule 4: Must have valid payment method (if available)
            if (_paymentMethodRepository is not null && _subscription.PaymentMethodId.HasValue)
            {
                var paymentMethod = await _paymentMethodRepository
                    .GetByIdAsync(_subscription.PaymentMethodId.Value, cancellationToken);

                if (paymentMethod is null || paymentMethod.IsExpired)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Enforces subscription usage limit rules.
    /// Ensures usage stays within plan limits.
    /// 
    /// NOTE: This rule requires Plan aggregate to have usage limit properties.
    /// If Plan doesn't have these properties yet, this rule will allow all usage
    /// until Plan is enhanced with HoneypotLimit, StorageLimit, UserLimit properties.
    /// </summary>
    public class SubscriptionUsageLimitRule : IAsyncBusinessRule
    {
        private readonly Subscription _subscription;
        private readonly UsageStatistics _newUsage;
        private readonly IPlanRepository _planRepository;
        private Error? _specificError;

        public SubscriptionUsageLimitRule(
            Subscription subscription,
            UsageStatistics newUsage,
            IPlanRepository planRepository)
        {
            _subscription = subscription;
            _newUsage = newUsage;
            _planRepository = planRepository;
        }

        public Error Error => _specificError ?? SubscriptionErrors.SubscriptionQuotaExceeded;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            var plan = await _planRepository
                .GetByIdAsync(_subscription.PlanId, cancellationToken);

            if (plan is null)
            {
                _specificError = SubscriptionErrors.PlanNotFound;
                return false;
            }

            // Validate honeypots usage
            // NOTE: This assumes Plan will have MaxHoneypots property
            // For now, we use simple limits based on plan type
            var maxHoneypots = GetMaxHoneypotsByPlanType(plan.Type);
            if (_newUsage.HoneypotsUsed > maxHoneypots)
            {
                _specificError = SubscriptionErrors.HoneypotsUsageExceeded;
                return false;
            }

            // Validate storage usage
            // NOTE: This assumes Plan will have MaxStorageGb property
            var maxStorageGb = GetMaxStorageByPlanType(plan.Type);
            if (_newUsage.StorageUsedGb > maxStorageGb)
            {
                _specificError = SubscriptionErrors.StorageUsageExceeded;
                return false;
            }

            // All validations passed
            return true;
        }

        /// <summary>
        /// Get maximum honeypots allowed by plan type.
        /// TODO: Replace with actual Plan.MaxHoneypots property when available.
        /// This is a simplified implementation based on basic plan types.
        /// For Custom plans, this should query plan-specific limits.
        /// </summary>
        private static int GetMaxHoneypotsByPlanType(PlanType planType)
        {
            return planType switch
            {
                PlanType.Free => 1,
                PlanType.Trial => 2,
                PlanType.Paid => 10,  // Default for paid plans
                PlanType.Custom => 100, // Custom plans negotiated separately
                _ => 1 // Default to most restrictive
            };
        }

        /// <summary>
        /// Get maximum storage (GB) allowed by plan type.
        /// TODO: Replace with actual Plan.MaxStorageGb property when available.
        /// This is a simplified implementation based on basic plan types.
        /// For Custom plans, this should query plan-specific limits.
        /// </summary>
        private static decimal GetMaxStorageByPlanType(PlanType planType)
        {
            return planType switch
            {
                PlanType.Free => 1,
                PlanType.Trial => 5,
                PlanType.Paid => 50,  // Default for paid plans
                PlanType.Custom => 1000, // Custom plans negotiated separately
                _ => 1 // Default to most restrictive
            };
        }
    }

    /// <summary>
    /// Enforces subscription status transition rules.
    /// Ensures valid status transitions.
    /// </summary>
    public class SubscriptionStatusTransitionRule : IBusinessRule
    {
        private readonly SubscriptionStatus _currentStatus;
        private readonly SubscriptionStatus _requestedStatus;

        public SubscriptionStatusTransitionRule(
            SubscriptionStatus currentStatus,
            SubscriptionStatus requestedStatus)
        {
            _currentStatus = currentStatus;
            _requestedStatus = requestedStatus;
        }

        public Error Error => SubscriptionErrors.InvalidStatusTransition;

        public bool IsSatisfied()
        {
            // Define valid transitions
            return (_currentStatus, _requestedStatus) switch
            {
                // Trial ? Active
                (SubscriptionStatus.Trial, SubscriptionStatus.Active) => true,
                // Trial ? Cancelled
                (SubscriptionStatus.Trial, SubscriptionStatus.Cancelled) => true,
                
                // Active ? Suspended
                (SubscriptionStatus.Active, SubscriptionStatus.Suspended) => true,
                // Active ? Cancelled
                (SubscriptionStatus.Active, SubscriptionStatus.Cancelled) => true,
                // Active ? Expired (automatic)
                (SubscriptionStatus.Active, SubscriptionStatus.Expired) => true,
                
                // Suspended ? Active
                (SubscriptionStatus.Suspended, SubscriptionStatus.Active) => true,
                // Suspended ? Cancelled
                (SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled) => true,
                
                // Expired ? Cancelled
                (SubscriptionStatus.Expired, SubscriptionStatus.Cancelled) => true,
                
                // Cannot transition FROM Cancelled
                (SubscriptionStatus.Cancelled, _) => false,
                
                // Default: invalid transition
                _ => false
            };
        }
    }

    /// <summary>
    /// Enforces subscription cancellation requirements.
    /// Ensures proper cancellation process.
    /// </summary>
    public class SubscriptionCancellationRule : IBusinessRule
    {
        private readonly Subscription _subscription;
        private readonly string _cancellationReason;

        public SubscriptionCancellationRule(
            Subscription subscription,
            string cancellationReason)
        {
            _subscription = subscription;
            _cancellationReason = cancellationReason;
        }

        public Error Error => SubscriptionErrors.InvalidCancellationReason;

        public bool IsSatisfied()
        {
            // Rule 1: Can only cancel non-cancelled subscriptions
            if (_subscription.Status == SubscriptionStatus.Cancelled)
                return false;

            // Rule 2: Cancellation reason required
            if (string.IsNullOrWhiteSpace(_cancellationReason))
                return false;

            // Rule 3: Reason must be minimum length
            if (_cancellationReason.Length < 5)
                return false;

            // Rule 4: Reason must not exceed maximum length
            if (_cancellationReason.Length > 500)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces subscription upgrade/downgrade rules.
    /// Ensures plan change is valid.
    /// </summary>
    public class SubscriptionPlanChangeRule : IAsyncBusinessRule
    {
        private const decimal MaxPersistableStorageGb = 99999999999999.9999m;

        private readonly Subscription _subscription;
        private readonly Plan _newPlan;
        private readonly IPlanRepository _planRepository;
        private Error? _specificError;

        public SubscriptionPlanChangeRule(
            Subscription subscription,
            Plan newPlan,
            IPlanRepository planRepository)
        {
            _subscription = subscription;
            _newPlan = newPlan;
            _planRepository = planRepository;
        }

        public Error Error => _specificError ?? SubscriptionErrors.SubscriptionPlanChangeNotAllowed;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Rule 1: Cannot change plan if subscription is cancelled
            if (_subscription.Status == SubscriptionStatus.Cancelled)
            {
                _specificError = SubscriptionErrors.SubscriptionPlanChangeNotAllowed;
                return false;
            }

            // Rule 2: Cannot change plan if subscription is expired
            if (_subscription.Status == SubscriptionStatus.Expired)
            {
                _specificError = SubscriptionErrors.SubscriptionPlanChangeNotAllowed;
                return false;
            }

            // Rule 3: New plan must be active
            if (!_newPlan.IsActive)
            {
                _specificError = SubscriptionErrors.SubscriptionPlanChangeNotAllowed;
                return false;
            }

            // Rule 4: Current plan must exist for proper downgrade detection.
            var currentPlan = await _planRepository
                .GetByIdAsync(_subscription.PlanId, cancellationToken);

            if (currentPlan is null)
            {
                _specificError = PlanErrors.PlanNotFound;
                return false;
            }

            // Rule 5: Current usage must fit target quota limits.
            var targetLimits = ResolveEffectiveQuotaLimits(_newPlan.QuotaDefinition);
            var usage = _subscription.GetQuotaUsageSummary();

            var exceedsTargetLimits =
                usage.CurrentHoneypots > targetLimits.MaxHoneypots ||
                usage.CurrentStorageGb > targetLimits.MaxStorageGb ||
                usage.CurrentApiCalls > targetLimits.MaxMonthlyApiCalls;

            if (!exceedsTargetLimits)
            {
                return true;
            }

            var currentQuota = _subscription.Quota;
            var isCustomizationDowngrade = currentPlan.CustomizationLevel > _newPlan.CustomizationLevel;
            var isQuotaDowngrade =
                currentQuota is null ||
                targetLimits.MaxHoneypots < currentQuota.MaxHoneypots ||
                targetLimits.MaxStorageGb < currentQuota.MaxStorageGb ||
                targetLimits.MaxMonthlyApiCalls < currentQuota.MaxMonthlyApiCalls ||
                targetLimits.MaxUsers < currentQuota.MaxUsers;

            if (isCustomizationDowngrade || isQuotaDowngrade)
            {
                _specificError = SubscriptionErrors.CannotDowngradeWithHighUsage;
                return false;
            }

            _specificError = SubscriptionErrors.SubscriptionPlanChangeNotAllowed;
            return false;
        }

        private static QuotaLimits ResolveEffectiveQuotaLimits(PlanQuotaDefinition? quotaDefinition)
        {
            var maxHoneypots = quotaDefinition is null || quotaDefinition.MaxHoneypots <= 0
                ? int.MaxValue
                : quotaDefinition.MaxHoneypots;

            var maxStorageGb = quotaDefinition is null || quotaDefinition.MaxStorageGb <= 0
                ? MaxPersistableStorageGb
                : Math.Min(quotaDefinition.MaxStorageGb, MaxPersistableStorageGb);

            var maxMonthlyApiCalls = quotaDefinition is null || quotaDefinition.MaxMonthlyApiCalls <= 0
                ? int.MaxValue
                : quotaDefinition.MaxMonthlyApiCalls;

            var maxUsers = quotaDefinition is null || quotaDefinition.MaxUsers <= 0
                ? int.MaxValue
                : quotaDefinition.MaxUsers;

            return new QuotaLimits(maxHoneypots, maxStorageGb, maxMonthlyApiCalls, maxUsers);
        }

        private readonly record struct QuotaLimits(
            int MaxHoneypots,
            decimal MaxStorageGb,
            int MaxMonthlyApiCalls,
            int MaxUsers);
    }

    /// <summary>
    /// Enforces payment method requirements for subscription operations.
    /// </summary>
    public class SubscriptionPaymentMethodRule : IBusinessRule
    {
        private readonly Subscription _subscription;

        public SubscriptionPaymentMethodRule(Subscription subscription)
        {
            _subscription = subscription;
        }

        public Error Error => SubscriptionErrors.InvalidPaymentMethod;

        public bool IsSatisfied()
        {
            // Rule 1: Paid subscriptions must have payment method
            // Note: Would need to get plan type, simplified here

            // Rule 2: Payment method ID must not be empty
            if (_subscription.PaymentMethodId == Guid.Empty)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces auto-renewal requirements.
    /// </summary>
    public class SubscriptionAutoRenewalRule : IBusinessRule
    {
        private readonly Subscription _subscription;

        public SubscriptionAutoRenewalRule(Subscription subscription)
        {
            _subscription = subscription;
        }

        public Error Error => SubscriptionErrors.CannotEnableAutoRenewalOnExpiring;

        public bool IsSatisfied()
        {
            // Rule 1: Cannot enable auto-renewal on cancelled subscriptions
            if (_subscription.Status == SubscriptionStatus.Cancelled)
                return false;

            // Rule 2: Cannot enable auto-renewal on expired subscriptions
            if (_subscription.Status == SubscriptionStatus.Expired)
                return false;

            return true;
        }
    }
}
