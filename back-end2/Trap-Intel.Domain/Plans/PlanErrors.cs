using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Error codes and factory methods for the Plans domain.
    /// Follows DDD error handling patterns with semantic error codes.
    /// </summary>
    public static class PlanErrors
    {
        // Name validation errors
        public static readonly Error InvalidName = Error.Custom(
            "Plan.InvalidName",
            "Plan name cannot be empty or exceed 255 characters.");

        public static readonly Error DuplicateName = Error.Custom(
            "Plan.DuplicateName",
            "A plan with this name already exists.");

        public static readonly Error NameTooShort = Error.Custom(
            "Plan.NameTooShort",
            "Plan name must be at least 3 characters long.");

        // Description validation errors
        public static readonly Error InvalidDescription = Error.Custom(
            "Plan.InvalidDescription",
            "Plan description cannot be empty.");

        public static readonly Error DescriptionTooShort = Error.Custom(
            "Plan.DescriptionTooShort",
            "Plan description must be at least 10 characters long.");

        // Type validation errors
        public static readonly Error InvalidType = Error.Custom(
            "Plan.InvalidType",
            "Invalid plan type specified.");

        // Configuration validation errors
        public static readonly Error InvalidSupportTier = Error.Custom(
            "Plan.InvalidSupportTier",
            "Support tier must be specified.");

        public static readonly Error InvalidSupportTierConfig = Error.Custom(
            "Plan.InvalidSupportTierConfig",
            "Support tier configuration is invalid.");

        public static readonly Error InvalidResponseTime = Error.Custom(
            "Plan.InvalidResponseTime",
            "Response time must be greater than 0 minutes.");

        public static readonly Error InvalidCompliance = Error.Custom(
            "Plan.InvalidCompliance",
            "Compliance configuration must be specified.");

        public static readonly Error InvalidComplianceConfig = Error.Custom(
            "Plan.InvalidComplianceConfig",
            "Compliance configuration is invalid.");

        public static readonly Error NoCertificationsProvided = Error.Custom(
            "Plan.NoCertificationsProvided",
            "At least one certification must be provided for compliance level.");

        // Pricing validation errors
        public static readonly Error InvalidPrice = Error.Custom(
            "Plan.InvalidPrice",
            "Price cannot be null or negative.");

        public static readonly Error PriceCannotBeNegative = Error.Custom(
            "Plan.PriceCannotBeNegative",
            "Price cannot be negative.");

        public static readonly Error InvalidSetupFee = Error.Custom(
            "Plan.InvalidSetupFee",
            "Setup fee cannot be negative.");

        public static readonly Error InvalidCurrency = Error.Custom(
            "Plan.InvalidCurrency",
            "Currency must be a valid ISO 4217 code (e.g., USD, EUR).");

        public static readonly Error PricingNotFound = Error.Custom(
            "Plan.PricingNotFound",
            "No pricing found for the specified billing cycle.");

        public static readonly Error PricingAlreadyExists = Error.Custom(
            "Plan.PricingAlreadyExists",
            "Pricing for this billing cycle already exists.");

        public static readonly Error CannotRemovePricing = Error.Custom(
            "Plan.CannotRemovePricing",
            "Cannot remove pricing while subscriptions are active.");

        // State transition errors
        public static readonly Error AlreadyActive = Error.Custom(
            "Plan.AlreadyActive",
            "Plan is already active.");

        public static readonly Error AlreadyInactive = Error.Custom(
            "Plan.AlreadyInactive",
            "Plan is already inactive.");

        public static readonly Error CannotDeactivateWithActiveSubscriptions = Error.Custom(
            "Plan.CannotDeactivateWithActiveSubscriptions",
            "Cannot deactivate a plan with active subscriptions.");

        // Feature validation errors
        public static readonly Error InvalidAIFeatures = Error.Custom(
            "Plan.InvalidAIFeatures",
            "AI features configuration is invalid.");

        public static readonly Error InvalidThreatIntelligence = Error.Custom(
            "Plan.InvalidThreatIntelligence",
            "Threat intelligence configuration is invalid.");

        public static readonly Error NoDataSourcesProvided = Error.Custom(
            "Plan.NoDataSourcesProvided",
            "At least one data source must be provided for threat intelligence.");

        public static readonly Error InvalidUpdateFrequency = Error.Custom(
            "Plan.InvalidUpdateFrequency",
            "Update frequency must be greater than 0 hours.");

        // Operation errors
        public static readonly Error InvalidOperation = Error.Custom(
            "Plan.InvalidOperation",
            "The requested operation is invalid for the current plan state.");

        public static readonly Error CannotModifyActivePlan = Error.Custom(
            "Plan.CannotModifyActivePlan",
            "Cannot modify core properties of an active plan. Deactivate it first.");

        public static readonly Error PlanNotFound = Error.Custom(
            "Plan.NotFound",
            "The requested plan does not exist.");

        // Customization errors
        public static readonly Error InvalidCustomizationLevel = Error.Custom(
            "Plan.InvalidCustomizationLevel",
            "Invalid customization level specified.");

        public static readonly Error EnterpriseCustomizationNotAllowed = Error.Custom(
            "Plan.EnterpriseCustomizationNotAllowed",
            "Enterprise customization is not available for this plan type.");

        // Billing cycle errors
        public static readonly Error InvalidBillingCycle = Error.Custom(
            "Plan.InvalidBillingCycle",
            "Invalid billing cycle specified.");

        public static readonly Error NoBillingCyclesConfigured = Error.Custom(
            "Plan.NoBillingCyclesConfigured",
            "At least one billing cycle must be configured.");

        public static readonly Error DuplicateBillingCycle = Error.Custom(
            "Plan.DuplicateBillingCycle",
            "Billing cycle is already configured for this plan.");
    }
}
