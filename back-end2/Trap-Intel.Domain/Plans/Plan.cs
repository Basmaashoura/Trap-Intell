using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans.ValueObjects;

namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Represents a subscription plan offered by the platform.
    /// Enterprise-grade design with factory methods, validation, and domain events.
    /// </summary>
    public class Plan : AggregateRoot<Guid>
    {
        private readonly Dictionary<BillingCycle, PlanPrice> _pricing = new();
        private List<PlanFeature> _features = new();

        private Plan() { }

        private Plan(
            Guid id,
            string name,
            string description,
            PlanType type,
            SupportTierConfig supportTier,
            ComplianceConfig complianceConfig,
            CustomizationLevel customizationLevel)
            : base(id)
        {
            Name = name;
            Description = description;
            Type = type;
            SupportTier = supportTier;
            ComplianceConfig = complianceConfig;
            CustomizationLevel = customizationLevel;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public PlanType Type { get; private set; }
        public SupportTierConfig SupportTier { get; private set; } = null!;
        public ComplianceConfig ComplianceConfig { get; private set; } = null!;
        public CustomizationLevel CustomizationLevel { get; private set; }
        public AIFeaturesConfig? AIFeatures { get; private set; }
        public ThreatIntelligenceConfig? ThreatIntelligence { get; private set; }
        
        /// <summary>
        /// Quota limits for this plan.
        /// </summary>
        public PlanQuotaDefinition? QuotaDefinition { get; private set; }
        
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyDictionary<BillingCycle, PlanPrice> Pricing => _pricing;
        
        /// <summary>
        /// Features included in this plan.
        /// </summary>
        public IReadOnlyList<PlanFeature> Features => _features.AsReadOnly();

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new plan with full validation.
        /// </summary>
        public static Result<Plan> Create(
            string name,
            string description,
            PlanType type,
            SupportTierConfig supportTier,
            ComplianceConfig complianceConfig,
            CustomizationLevel customizationLevel,
            PlanQuotaDefinition? quotaDefinition = null,
            List<PlanFeature>? features = null)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Plan>(Error.Custom("Plan.InvalidName", "Plan name cannot be empty."));

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<Plan>(Error.Custom("Plan.InvalidDescription", "Plan description cannot be empty."));

            if (supportTier is null)
                return Result.Failure<Plan>(Error.Custom("Plan.InvalidSupportTier", "Support tier must be specified."));

            if (complianceConfig is null)
                return Result.Failure<Plan>(Error.Custom("Plan.InvalidCompliance", "Compliance config must be specified."));

            var plan = new Plan(
                Guid.NewGuid(),
                name.Trim(),
                description.Trim(),
                type,
                supportTier,
                complianceConfig,
                customizationLevel)
            {
                QuotaDefinition = quotaDefinition,
                _features = features ?? new()
            };

            // Raise domain event
            plan.RaiseDomainEvent(new PlanCreatedEvent(
                plan.Id,
                plan.Name,
                plan.Type,
                DateTime.UtcNow));

            return Result.Success(plan);
        }

        /// <summary>
        /// Factory method to reconstruct plan from database.
        /// </summary>
        public static Plan Reconstruct(
            Guid id,
            string name,
            string description,
            PlanType type,
            SupportTierConfig supportTier,
            ComplianceConfig complianceConfig,
            CustomizationLevel customizationLevel,
            AIFeaturesConfig? aiFeatures,
            ThreatIntelligenceConfig? threatIntel,
            PlanQuotaDefinition? quotaDefinition,
            bool isActive,
            DateTime createdAt,
            DateTime updatedAt,
            List<PlanFeature>? features = null)
        {
            var plan = new Plan(
                id,
                name,
                description,
                type,
                supportTier,
                complianceConfig,
                customizationLevel)
            {
                AIFeatures = aiFeatures,
                ThreatIntelligence = threatIntel,
                QuotaDefinition = quotaDefinition,
                IsActive = isActive,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                _features = features ?? new()
            };

            return plan;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Add pricing for a billing cycle.
        /// </summary>
        public Result AddPricing(BillingCycle cycle, PlanPrice price)
        {
            if (price is null)
                return Result.Failure(Error.Custom("Plan.InvalidPrice", "Price cannot be null."));

            if (price.Amount < 0)
                return Result.Failure(Error.Custom("Plan.InvalidPrice", "Price cannot be negative."));

            _pricing[cycle] = price;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PlanPricingAddedEvent(
                Id,
                cycle,
                price.Amount,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Remove pricing for a billing cycle.
        /// </summary>
        public Result RemovePricing(BillingCycle cycle)
        {
            if (!_pricing.ContainsKey(cycle))
                return Result.Failure(Error.Custom("Plan.PricingNotFound", $"No pricing found for {cycle}."));

            _pricing.Remove(cycle);
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Get price for a specific billing cycle.
        /// </summary>
        public PlanPrice? GetPrice(BillingCycle cycle)
        {
            return _pricing.TryGetValue(cycle, out var price) ? price : null;
        }

        /// <summary>
        /// Enable AI features.
        /// </summary>
        public void EnableAIFeatures(AIFeaturesConfig aiFeatures)
        {
            if (aiFeatures is null)
                throw new ArgumentNullException(nameof(aiFeatures));

            AIFeatures = aiFeatures;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new AIFeaturesEnabledEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Enable threat intelligence.
        /// </summary>
        public void EnableThreatIntelligence(ThreatIntelligenceConfig threatIntel)
        {
            if (threatIntel is null)
                throw new ArgumentNullException(nameof(threatIntel));

            ThreatIntelligence = threatIntel;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ThreatIntelligenceEnabledEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Set quota definition for this plan.
        /// </summary>
        public Result SetQuotaDefinition(PlanQuotaDefinition quotaDefinition)
        {
            if (quotaDefinition is null)
                return Result.Failure(Error.Custom("Plan.InvalidQuota", "Quota definition cannot be null."));

            QuotaDefinition = quotaDefinition;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PlanQuotaDefinitionUpdatedEvent(
                Id,
                quotaDefinition.MaxHoneypots,
                quotaDefinition.MaxStorageGb,
                quotaDefinition.MaxUsers,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Set features for this plan.
        /// </summary>
        public Result SetFeatures(List<PlanFeature> features)
        {
            if (features is null)
                return Result.Failure(Error.Custom("Plan.InvalidFeatures", "Features list cannot be null."));

            _features = features;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PlanFeaturesUpdatedEvent(
                Id,
                features.Count,
                features.Count(f => f.IsEnabled),
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Add a feature to this plan.
        /// </summary>
        public Result AddFeature(PlanFeature feature)
        {
            if (feature is null)
                return Result.Failure(Error.Custom("Plan.InvalidFeature", "Feature cannot be null."));

            if (_features.Any(f => f.Code == feature.Code))
                return Result.Failure(Error.Custom("Plan.FeatureExists", $"Feature '{feature.Code}' already exists."));

            _features.Add(feature);
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Remove a feature from this plan.
        /// </summary>
        public Result RemoveFeature(string featureCode)
        {
            var feature = _features.FirstOrDefault(f => f.Code == featureCode);
            if (feature is null)
                return Result.Failure(Error.Custom("Plan.FeatureNotFound", $"Feature '{featureCode}' not found."));

            _features.Remove(feature);
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Check if plan has a specific feature enabled.
        /// </summary>
        public bool HasFeature(string featureCode)
        {
            return _features.Any(f => f.Code == featureCode && f.IsEnabled);
        }

        /// <summary>
        /// Get feature by code.
        /// </summary>
        public PlanFeature? GetFeature(string featureCode)
        {
            return _features.FirstOrDefault(f => f.Code == featureCode);
        }

        /// <summary>
        /// Get features by category.
        /// </summary>
        public IEnumerable<PlanFeature> GetFeaturesByCategory(FeatureCategory category)
        {
            return _features.Where(f => f.Category == category).OrderBy(f => f.SortOrder);
        }

        /// <summary>
        /// Get all enabled features.
        /// </summary>
        public IEnumerable<PlanFeature> GetEnabledFeatures()
        {
            return _features.Where(f => f.IsEnabled);
        }

        /// <summary>
        /// Get all premium features.
        /// </summary>
        public IEnumerable<PlanFeature> GetPremiumFeatures()
        {
            return _features.Where(f => f.IsPremium);
        }

        /// <summary>
        /// Activate the plan.
        /// </summary>
        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PlanActivatedEvent(Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Deactivate the plan.
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PlanDeactivatedEvent(Id, DateTime.UtcNow));
        }

        #endregion
    }
}
