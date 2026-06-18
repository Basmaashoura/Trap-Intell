namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Value objects for the Plans domain.
    /// </summary>
    
    /// <summary>
    /// Represents pricing for a plan in a specific billing cycle.
    /// </summary>
    public record PlanPrice(
        decimal Amount,
        string Currency = "USD",
        decimal SetupFee = 0);

    /// <summary>
    /// Represents feature limits in a plan.
    /// </summary>
    public record FeatureLimit(
        string FeatureName,
        long LimitValue,
        string Unit = "");

    /// <summary>
    /// Represents support tier configuration.
    /// </summary>
    public record SupportTierConfig(
        SupportLevel Level,
        int ResponseTimeMinutes,
        bool IncludesDedicatedManager = false);

    /// <summary>
    /// Represents compliance requirements.
    /// </summary>
    public record ComplianceConfig(
        ComplianceLevel Level,
        string[] RequiredCertifications,
        bool AuditingIncluded = false);

    /// <summary>
    /// Represents AI features configuration.
    /// </summary>
    public record AIFeaturesConfig(
        bool ThreatAnalysis,
        bool AutomatedDetection,
        bool PredictiveAnalytics,
        bool CustomModels = false);

    /// <summary>
    /// Represents threat intelligence access configuration.
    /// </summary>
    public record ThreatIntelligenceConfig(
        bool IsIncluded,
        string[] DataSources,
        int UpdateFrequencyHours = 24);

    /// <summary>
    /// Defines quota limits for a subscription plan.
    /// Used to initialize SubscriptionQuotaEntity when subscription is created.
    /// </summary>
    public record PlanQuotaDefinition
    {
        /// <summary>
        /// Maximum number of honeypots allowed.
        /// </summary>
        public int MaxHoneypots { get; init; }

        /// <summary>
        /// Maximum storage in GB.
        /// </summary>
        public decimal MaxStorageGb { get; init; }

        /// <summary>
        /// Maximum API calls per month.
        /// </summary>
        public int MaxMonthlyApiCalls { get; init; }

        /// <summary>
        /// Maximum users allowed in organization.
        /// </summary>
        public int MaxUsers { get; init; }

        /// <summary>
        /// Maximum attack events retained (0 = unlimited).
        /// </summary>
        public int MaxAttackEventsRetained { get; init; }

        /// <summary>
        /// Data retention period in days.
        /// </summary>
        public int DataRetentionDays { get; init; }

        /// <summary>
        /// Maximum reports per month.
        /// </summary>
        public int MaxMonthlyReports { get; init; }

        /// <summary>
        /// Maximum webhooks allowed.
        /// </summary>
        public int MaxWebhooks { get; init; }

        /// <summary>
        /// Maximum API keys allowed.
        /// </summary>
        public int MaxApiKeys { get; init; }

        /// <summary>
        /// Whether hard limits are enforced (vs soft limits with overage charges).
        /// </summary>
        public bool HardLimitEnforced { get; init; }

        /// <summary>
        /// Overage rate per additional honeypot.
        /// </summary>
        public decimal OverageHoneypotRate { get; init; }

        /// <summary>
        /// Overage rate per additional GB of storage.
        /// </summary>
        public decimal OverageStorageRatePerGb { get; init; }

        /// <summary>
        /// Overage rate per 1000 additional API calls.
        /// </summary>
        public decimal OverageApiCallRatePer1000 { get; init; }

        public PlanQuotaDefinition(
            int maxHoneypots,
            decimal maxStorageGb,
            int maxMonthlyApiCalls = 100000,
            int maxUsers = 10,
            int maxAttackEventsRetained = 0,
            int dataRetentionDays = 90,
            int maxMonthlyReports = 10,
            int maxWebhooks = 5,
            int maxApiKeys = 5,
            bool hardLimitEnforced = false,
            decimal overageHoneypotRate = 10m,
            decimal overageStorageRatePerGb = 0.50m,
            decimal overageApiCallRatePer1000 = 0.10m)
        {
            MaxHoneypots = maxHoneypots > 0 ? maxHoneypots : 1;
            MaxStorageGb = maxStorageGb > 0 ? maxStorageGb : 1;
            MaxMonthlyApiCalls = maxMonthlyApiCalls > 0 ? maxMonthlyApiCalls : 1000;
            MaxUsers = maxUsers > 0 ? maxUsers : 1;
            MaxAttackEventsRetained = maxAttackEventsRetained;
            DataRetentionDays = dataRetentionDays > 0 ? dataRetentionDays : 30;
            MaxMonthlyReports = maxMonthlyReports > 0 ? maxMonthlyReports : 1;
            MaxWebhooks = maxWebhooks > 0 ? maxWebhooks : 1;
            MaxApiKeys = maxApiKeys > 0 ? maxApiKeys : 1;
            HardLimitEnforced = hardLimitEnforced;
            OverageHoneypotRate = overageHoneypotRate;
            OverageStorageRatePerGb = overageStorageRatePerGb;
            OverageApiCallRatePer1000 = overageApiCallRatePer1000;
        }

        #region Factory Methods

        /// <summary>
        /// Free tier quota definition.
        /// </summary>
        public static PlanQuotaDefinition FreeTier() => new(
            maxHoneypots: 2,
            maxStorageGb: 1,
            maxMonthlyApiCalls: 1000,
            maxUsers: 2,
            maxAttackEventsRetained: 1000,
            dataRetentionDays: 7,
            maxMonthlyReports: 2,
            maxWebhooks: 1,
            maxApiKeys: 1,
            hardLimitEnforced: true);

        /// <summary>
        /// Starter tier quota definition.
        /// </summary>
        public static PlanQuotaDefinition StarterTier() => new(
            maxHoneypots: 5,
            maxStorageGb: 10,
            maxMonthlyApiCalls: 10000,
            maxUsers: 5,
            maxAttackEventsRetained: 10000,
            dataRetentionDays: 30,
            maxMonthlyReports: 10,
            maxWebhooks: 3,
            maxApiKeys: 3,
            hardLimitEnforced: false,
            overageHoneypotRate: 15m,
            overageStorageRatePerGb: 1m);

        /// <summary>
        /// Professional tier quota definition.
        /// </summary>
        public static PlanQuotaDefinition ProfessionalTier() => new(
            maxHoneypots: 20,
            maxStorageGb: 50,
            maxMonthlyApiCalls: 100000,
            maxUsers: 25,
            maxAttackEventsRetained: 100000,
            dataRetentionDays: 90,
            maxMonthlyReports: 50,
            maxWebhooks: 10,
            maxApiKeys: 10,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.50m);

        /// <summary>
        /// Enterprise tier quota definition.
        /// </summary>
        public static PlanQuotaDefinition EnterpriseTier() => new(
            maxHoneypots: 100,
            maxStorageGb: 500,
            maxMonthlyApiCalls: 1000000,
            maxUsers: 100,
            maxAttackEventsRetained: 0, // Unlimited
            dataRetentionDays: 365,
            maxMonthlyReports: 0, // Unlimited
            maxWebhooks: 50,
            maxApiKeys: 50,
            hardLimitEnforced: false,
            overageHoneypotRate: 5m,
            overageStorageRatePerGb: 0.25m);

        /// <summary>
        /// Unlimited tier (custom enterprise).
        /// </summary>
        public static PlanQuotaDefinition Unlimited() => new(
            maxHoneypots: int.MaxValue,
            // Keep "unlimited" within database decimal(18,4) precision to avoid overflow on persistence.
            maxStorageGb: 99999999999999.9999m,
            maxMonthlyApiCalls: int.MaxValue,
            maxUsers: int.MaxValue,
            maxAttackEventsRetained: 0,
            dataRetentionDays: int.MaxValue,
            maxMonthlyReports: int.MaxValue,
            maxWebhooks: int.MaxValue,
            maxApiKeys: int.MaxValue,
            hardLimitEnforced: false);

        #endregion

        #region Query Methods

        /// <summary>
        /// Check if honeypot limit allows adding more.
        /// </summary>
        public bool CanAddHoneypot(int currentCount) => currentCount < MaxHoneypots;

        /// <summary>
        /// Check if user limit allows adding more.
        /// </summary>
        public bool CanAddUser(int currentCount) => currentCount < MaxUsers;

        /// <summary>
        /// Check if webhook limit allows adding more.
        /// </summary>
        public bool CanAddWebhook(int currentCount) => currentCount < MaxWebhooks;

        /// <summary>
        /// Check if API key limit allows adding more.
        /// </summary>
        public bool CanAddApiKey(int currentCount) => currentCount < MaxApiKeys;

        /// <summary>
        /// Calculate overage charges for given usage.
        /// </summary>
        public decimal CalculateOverageCharges(
            int honeypotsUsed,
            decimal storageUsedGb,
            int apiCallsUsed)
        {
            if (HardLimitEnforced)
                return 0;

            decimal charges = 0;

            if (honeypotsUsed > MaxHoneypots)
                charges += (honeypotsUsed - MaxHoneypots) * OverageHoneypotRate;

            if (storageUsedGb > MaxStorageGb)
                charges += (storageUsedGb - MaxStorageGb) * OverageStorageRatePerGb;

            if (apiCallsUsed > MaxMonthlyApiCalls)
            {
                var extraCalls = apiCallsUsed - MaxMonthlyApiCalls;
                charges += (extraCalls / 1000m) * OverageApiCallRatePer1000;
            }

            return Math.Round(charges, 2);
        }

        #endregion
    }
}
