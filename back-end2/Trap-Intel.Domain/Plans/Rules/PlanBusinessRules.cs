using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Enforces plan activation requirements.
    /// Ensures plan is properly configured before activation.
    /// </summary>
    public class PlanActivationRule : IBusinessRule
    {
        private readonly Plan _plan;

        public PlanActivationRule(Plan plan)
        {
            _plan = plan;
        }

        public Error Error => PlanErrors.InvalidOperation;

        public bool IsSatisfied()
        {
            // Rule 1: Plan must have at least one pricing configuration
            if (_plan.Pricing.Count == 0)
                return false;

            // Rule 2: Paid plans must have valid pricing (non-zero)
            if (_plan.Type == PlanType.Paid)
            {
                var hasPaidPricing = _plan.Pricing.Values.Any(p => p.Amount > 0);
                if (!hasPaidPricing)
                    return false;
            }

            // Rule 3: Must have support tier configured
            if (_plan.SupportTier is null)
                return false;

            // Rule 4: Free plans cannot have expensive support
            if (_plan.Type == PlanType.Free && 
                _plan.SupportTier.Level == SupportLevel.Dedicated)
                return false;

            // Rule 5: Must have compliance configuration
            if (_plan.ComplianceConfig is null)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces plan pricing rules.
    /// Validates pricing configuration based on business policies.
    /// </summary>
    public class PlanPricingRule : IBusinessRule
    {
        private readonly BillingCycle _cycle;
        private readonly PlanPrice _price;
        private readonly PlanType _planType;

        public PlanPricingRule(BillingCycle cycle, PlanPrice price, PlanType planType)
        {
            _cycle = cycle;
            _price = price;
            _planType = planType;
        }

        public Error Error => PlanErrors.InvalidPrice;

        public bool IsSatisfied()
        {
            // Rule 1: Price cannot be null
            if (_price is null)
                return false;

            // Rule 2: Price cannot be negative
            if (_price.Amount < 0)
                return false;

            // Rule 3: Free plans must have zero price
            if (_planType == PlanType.Free && _price.Amount > 0)
                return false;

            // Rule 4: Setup fees only allowed for annual billing
            if (_cycle != BillingCycle.Annually && _price.SetupFee > 0)
                return false;

            // Rule 5: Setup fee cannot be negative
            if (_price.SetupFee < 0)
                return false;

            // Rule 6: Currency must be valid
            if (string.IsNullOrWhiteSpace(_price.Currency))
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces plan feature configuration rules.
    /// Ensures features match plan tier.
    /// </summary>
    public class PlanFeatureConfigurationRule : IBusinessRule
    {
        private readonly Plan _plan;

        public PlanFeatureConfigurationRule(Plan plan)
        {
            _plan = plan;
        }

        public Error Error => PlanErrors.InvalidAIFeatures;

        public bool IsSatisfied()
        {
            // Rule 1: Free plans cannot have AI features
            if (_plan.Type == PlanType.Free && _plan.AIFeatures is not null)
            {
                if (_plan.AIFeatures.ThreatAnalysis || 
                    _plan.AIFeatures.PredictiveAnalytics)
                    return false;
            }

            // Rule 2: Trial plans cannot have custom models
            if (_plan.Type == PlanType.Trial && 
                _plan.AIFeatures?.CustomModels == true)
                return false;

            // Rule 3: Threat intelligence requires at least one data source
            if (_plan.ThreatIntelligence?.IsIncluded == true)
            {
                if (_plan.ThreatIntelligence.DataSources is null || 
                    _plan.ThreatIntelligence.DataSources.Length == 0)
                    return false;
            }

            // Rule 4: Custom plans can have all features
            // (No restrictions)

            return true;
        }
    }

    /// <summary>
    /// Enforces plan deactivation requirements.
    /// Ensures safe deactivation.
    /// </summary>
    public class PlanDeactivationRule : IAsyncBusinessRule
    {
        private readonly Plan _plan;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public PlanDeactivationRule(
            Plan plan,
            ISubscriptionRepository subscriptionRepository)
        {
            _plan = plan;
            _subscriptionRepository = subscriptionRepository;
        }

        public Error Error => PlanErrors.CannotDeactivateWithActiveSubscriptions;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Rule 1: Can only deactivate active plans
            if (!_plan.IsActive)
                return true; // Already inactive

            // Rule 2: Cannot deactivate if has active subscriptions
            var activeSubscriptions = await _subscriptionRepository
                .CountByPlanAsync(_plan.Id, SubscriptionStatus.Active, cancellationToken);

            if (activeSubscriptions > 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces plan modification rules.
    /// Prevents modification of plans with active subscriptions.
    /// </summary>
    public class PlanModificationRule : IAsyncBusinessRule
    {
        private readonly Plan _plan;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly string _propertyName;

        public PlanModificationRule(
            Plan plan,
            ISubscriptionRepository subscriptionRepository,
            string propertyName)
        {
            _plan = plan;
            _subscriptionRepository = subscriptionRepository;
            _propertyName = propertyName;
        }

        public Error Error => PlanErrors.CannotModifyActivePlan;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Rule: Cannot modify core properties if has active subscriptions
            var coreProperties = new[] { "Name", "Type", "CustomizationLevel" };

            if (!coreProperties.Contains(_propertyName))
                return true; // Modification allowed for non-core properties

            if (!_plan.IsActive)
                return true; // Not active, can modify

            // Check if plan has active subscriptions
            var activeCount = await _subscriptionRepository
                .CountByPlanAsync(_plan.Id, SubscriptionStatus.Active, cancellationToken);

            return activeCount == 0;
        }
    }
}
