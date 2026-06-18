namespace Trap_Intel.Domain.Plans.ValueObjects;

/// <summary>
/// Represents a feature included in a plan.
/// Enables feature flag management and plan comparison.
/// </summary>
public record PlanFeature
{
    /// <summary>
    /// Unique feature code (e.g., "ai_threat_analysis").
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Display name for the feature.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Description of the feature.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Category for grouping features.
    /// </summary>
    public FeatureCategory Category { get; init; }

    /// <summary>
    /// Whether the feature is enabled in this plan.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Limit value if feature has usage limits (null = unlimited).
    /// </summary>
    public int? LimitValue { get; init; }

    /// <summary>
    /// Unit for the limit (e.g., "per month", "total").
    /// </summary>
    public string? LimitUnit { get; init; }

    /// <summary>
    /// Whether this is a premium/highlighted feature.
    /// </summary>
    public bool IsPremium { get; init; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; init; }

    public PlanFeature(
        string code,
        string name,
        string description,
        FeatureCategory category,
        bool isEnabled = true,
        int? limitValue = null,
        string? limitUnit = null,
        bool isPremium = false,
        int sortOrder = 0)
    {
        Code = code?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Category = category;
        IsEnabled = isEnabled;
        LimitValue = limitValue;
        LimitUnit = limitUnit;
        IsPremium = isPremium;
        SortOrder = sortOrder;
    }

    #region Standard Features

    // Honeypot Features
    public static PlanFeature SSHHoneypot(bool enabled = true) => new(
        "honeypot_ssh", "SSH Honeypot", "Deploy SSH honeypots to capture brute force attacks",
        FeatureCategory.Honeypots, enabled, sortOrder: 1);

    public static PlanFeature HTTPHoneypot(bool enabled = true) => new(
        "honeypot_http", "HTTP/HTTPS Honeypot", "Deploy web honeypots to capture web attacks",
        FeatureCategory.Honeypots, enabled, sortOrder: 2);

    public static PlanFeature DatabaseHoneypot(bool enabled = true) => new(
        "honeypot_database", "Database Honeypot", "Deploy MySQL/PostgreSQL honeypots",
        FeatureCategory.Honeypots, enabled, sortOrder: 3);

    public static PlanFeature CustomHoneypot(bool enabled = false) => new(
        "honeypot_custom", "Custom Honeypot", "Deploy custom protocol honeypots",
        FeatureCategory.Honeypots, enabled, isPremium: true, sortOrder: 10);

    // AI Features
    public static PlanFeature AIThreatAnalysis(bool enabled = false) => new(
        "ai_threat_analysis", "AI Threat Analysis", "ML-powered attack classification",
        FeatureCategory.AI, enabled, isPremium: true, sortOrder: 1);

    public static PlanFeature AIAnomalyDetection(bool enabled = false) => new(
        "ai_anomaly_detection", "Anomaly Detection", "Detect unusual attack patterns",
        FeatureCategory.AI, enabled, isPremium: true, sortOrder: 2);

    public static PlanFeature AIPredictiveAnalytics(bool enabled = false) => new(
        "ai_predictive", "Predictive Analytics", "Predict future attack trends",
        FeatureCategory.AI, enabled, isPremium: true, sortOrder: 3);

    public static PlanFeature AIRecommendations(bool enabled = false, int? limit = null) => new(
        "ai_recommendations", "AI Recommendations", "Get AI-powered security recommendations",
        FeatureCategory.AI, enabled, limit, limit.HasValue ? "per month" : null, isPremium: true, sortOrder: 4);

    // Threat Intelligence Features
    public static PlanFeature ThreatActorTracking(bool enabled = true) => new(
        "threat_actor_tracking", "Threat Actor Tracking", "Track and profile threat actors",
        FeatureCategory.ThreatIntelligence, enabled, sortOrder: 1);

    public static PlanFeature MITREMapping(bool enabled = true) => new(
        "mitre_mapping", "MITRE ATT&CK Mapping", "Map attacks to MITRE framework",
        FeatureCategory.ThreatIntelligence, enabled, sortOrder: 2);

    public static PlanFeature IOCExport(bool enabled = false) => new(
        "ioc_export", "IOC Export", "Export indicators of compromise",
        FeatureCategory.ThreatIntelligence, enabled, isPremium: true, sortOrder: 3);

    public static PlanFeature ThreatFeeds(bool enabled = false) => new(
        "threat_feeds", "External Threat Feeds", "Integration with external threat feeds",
        FeatureCategory.ThreatIntelligence, enabled, isPremium: true, sortOrder: 4);

    // Alerting Features
    public static PlanFeature EmailAlerts(bool enabled = true) => new(
        "alerts_email", "Email Alerts", "Receive alerts via email",
        FeatureCategory.Alerting, enabled, sortOrder: 1);

    public static PlanFeature SMSAlerts(bool enabled = false) => new(
        "alerts_sms", "SMS Alerts", "Receive critical alerts via SMS",
        FeatureCategory.Alerting, enabled, isPremium: true, sortOrder: 2);

    public static PlanFeature SlackIntegration(bool enabled = false) => new(
        "alerts_slack", "Slack Integration", "Send alerts to Slack channels",
        FeatureCategory.Alerting, enabled, sortOrder: 3);

    public static PlanFeature WebhookAlerts(bool enabled = true, int? limit = null) => new(
        "alerts_webhook", "Webhook Alerts", "Send alerts to custom webhooks",
        FeatureCategory.Alerting, enabled, limit, "webhooks", sortOrder: 4);

    public static PlanFeature PagerDutyIntegration(bool enabled = false) => new(
        "alerts_pagerduty", "PagerDuty Integration", "Integrate with PagerDuty",
        FeatureCategory.Alerting, enabled, isPremium: true, sortOrder: 5);

    // Reporting Features
    public static PlanFeature BasicReports(bool enabled = true, int? limit = null) => new(
        "reports_basic", "Basic Reports", "Generate standard security reports",
        FeatureCategory.Reporting, enabled, limit, "per month", sortOrder: 1);

    public static PlanFeature CustomReports(bool enabled = false) => new(
        "reports_custom", "Custom Reports", "Create custom report templates",
        FeatureCategory.Reporting, enabled, isPremium: true, sortOrder: 2);

    public static PlanFeature ScheduledReports(bool enabled = false) => new(
        "reports_scheduled", "Scheduled Reports", "Automatically generate reports on schedule",
        FeatureCategory.Reporting, enabled, isPremium: true, sortOrder: 3);

    public static PlanFeature ExecutiveReports(bool enabled = false) => new(
        "reports_executive", "Executive Reports", "High-level reports for leadership",
        FeatureCategory.Reporting, enabled, isPremium: true, sortOrder: 4);

    // API Features
    public static PlanFeature APIAccess(bool enabled = true, int? limit = null) => new(
        "api_access", "API Access", "Programmatic access to platform",
        FeatureCategory.API, enabled, limit, "calls/month", sortOrder: 1);

    public static PlanFeature APIKeys(bool enabled = true, int? limit = null) => new(
        "api_keys", "API Keys", "Create API keys for integration",
        FeatureCategory.API, enabled, limit, "keys", sortOrder: 2);

    public static PlanFeature GraphQLAPI(bool enabled = false) => new(
        "api_graphql", "GraphQL API", "Access data via GraphQL",
        FeatureCategory.API, enabled, isPremium: true, sortOrder: 3);

    // Compliance Features
    public static PlanFeature AuditLogs(bool enabled = true, int? retentionDays = null) => new(
        "compliance_audit", "Audit Logs", "Complete audit trail of all actions",
        FeatureCategory.Compliance, enabled, retentionDays, "days retention", sortOrder: 1);

    public static PlanFeature GDPRCompliance(bool enabled = false) => new(
        "compliance_gdpr", "GDPR Compliance", "GDPR-compliant data handling",
        FeatureCategory.Compliance, enabled, isPremium: true, sortOrder: 2);

    public static PlanFeature SOC2Compliance(bool enabled = false) => new(
        "compliance_soc2", "SOC 2 Compliance", "SOC 2 compliant controls",
        FeatureCategory.Compliance, enabled, isPremium: true, sortOrder: 3);

    public static PlanFeature HIPAACompliance(bool enabled = false) => new(
        "compliance_hipaa", "HIPAA Compliance", "HIPAA-compliant data handling",
        FeatureCategory.Compliance, enabled, isPremium: true, sortOrder: 4);

    // Support Features
    public static PlanFeature EmailSupport(bool enabled = true) => new(
        "support_email", "Email Support", "Support via email",
        FeatureCategory.Support, enabled, sortOrder: 1);

    public static PlanFeature ChatSupport(bool enabled = false) => new(
        "support_chat", "Live Chat Support", "Real-time chat support",
        FeatureCategory.Support, enabled, isPremium: true, sortOrder: 2);

    public static PlanFeature PhoneSupport(bool enabled = false) => new(
        "support_phone", "Phone Support", "Direct phone support line",
        FeatureCategory.Support, enabled, isPremium: true, sortOrder: 3);

    public static PlanFeature DedicatedCSM(bool enabled = false) => new(
        "support_csm", "Dedicated CSM", "Dedicated customer success manager",
        FeatureCategory.Support, enabled, isPremium: true, sortOrder: 4);

    public static PlanFeature SLA(string slaLevel, bool enabled = false) => new(
        "support_sla", $"SLA ({slaLevel})", $"{slaLevel} response time SLA",
        FeatureCategory.Support, enabled, isPremium: true, sortOrder: 5);

    #endregion

    #region Plan Feature Sets

    /// <summary>
    /// Get standard features for Free tier.
    /// </summary>
    public static List<PlanFeature> FreeTierFeatures() =>
    [
        SSHHoneypot(),
        HTTPHoneypot(),
        ThreatActorTracking(),
        MITREMapping(),
        EmailAlerts(),
        BasicReports(limit: 2),
        APIAccess(limit: 1000),
        APIKeys(limit: 1),
        AuditLogs(retentionDays: 7),
        EmailSupport()
    ];

    /// <summary>
    /// Get standard features for Starter tier.
    /// </summary>
    public static List<PlanFeature> StarterTierFeatures() =>
    [
        SSHHoneypot(),
        HTTPHoneypot(),
        DatabaseHoneypot(),
        ThreatActorTracking(),
        MITREMapping(),
        EmailAlerts(),
        SlackIntegration(),
        WebhookAlerts(limit: 3),
        BasicReports(limit: 10),
        APIAccess(limit: 10000),
        APIKeys(limit: 3),
        AuditLogs(retentionDays: 30),
        EmailSupport(),
        ChatSupport(enabled: true)
    ];

    /// <summary>
    /// Get standard features for Professional tier.
    /// </summary>
    public static List<PlanFeature> ProfessionalTierFeatures() =>
    [
        SSHHoneypot(),
        HTTPHoneypot(),
        DatabaseHoneypot(),
        CustomHoneypot(enabled: true),
        AIThreatAnalysis(enabled: true),
        AIAnomalyDetection(enabled: true),
        AIRecommendations(enabled: true, limit: 50),
        ThreatActorTracking(),
        MITREMapping(),
        IOCExport(enabled: true),
        EmailAlerts(),
        SMSAlerts(enabled: true),
        SlackIntegration(),
        WebhookAlerts(limit: 10),
        PagerDutyIntegration(enabled: true),
        BasicReports(limit: 50),
        CustomReports(enabled: true),
        ScheduledReports(enabled: true),
        APIAccess(limit: 100000),
        APIKeys(limit: 10),
        AuditLogs(retentionDays: 90),
        GDPRCompliance(enabled: true),
        EmailSupport(),
        ChatSupport(enabled: true),
        PhoneSupport(enabled: true),
        SLA("4-hour", enabled: true)
    ];

    /// <summary>
    /// Get standard features for Enterprise tier.
    /// </summary>
    public static List<PlanFeature> EnterpriseTierFeatures() =>
    [
        SSHHoneypot(),
        HTTPHoneypot(),
        DatabaseHoneypot(),
        CustomHoneypot(enabled: true),
        AIThreatAnalysis(enabled: true),
        AIAnomalyDetection(enabled: true),
        AIPredictiveAnalytics(enabled: true),
        AIRecommendations(enabled: true),
        ThreatActorTracking(),
        MITREMapping(),
        IOCExport(enabled: true),
        ThreatFeeds(enabled: true),
        EmailAlerts(),
        SMSAlerts(enabled: true),
        SlackIntegration(),
        WebhookAlerts(limit: 50),
        PagerDutyIntegration(enabled: true),
        BasicReports(),
        CustomReports(enabled: true),
        ScheduledReports(enabled: true),
        ExecutiveReports(enabled: true),
        APIAccess(limit: 1000000),
        APIKeys(limit: 50),
        GraphQLAPI(enabled: true),
        AuditLogs(retentionDays: 365),
        GDPRCompliance(enabled: true),
        SOC2Compliance(enabled: true),
        HIPAACompliance(enabled: true),
        EmailSupport(),
        ChatSupport(enabled: true),
        PhoneSupport(enabled: true),
        DedicatedCSM(enabled: true),
        SLA("1-hour", enabled: true)
    ];

    #endregion
}

/// <summary>
/// Categories for grouping plan features.
/// </summary>
public enum FeatureCategory
{
    /// <summary>Honeypot deployment features.</summary>
    Honeypots = 0,
    
    /// <summary>AI and machine learning features.</summary>
    AI = 1,
    
    /// <summary>Threat intelligence features.</summary>
    ThreatIntelligence = 2,
    
    /// <summary>Alerting and notification features.</summary>
    Alerting = 3,
    
    /// <summary>Reporting features.</summary>
    Reporting = 4,
    
    /// <summary>API access features.</summary>
    API = 5,
    
    /// <summary>Compliance features.</summary>
    Compliance = 6,
    
    /// <summary>Support features.</summary>
    Support = 7,
    
    /// <summary>Data management features.</summary>
    DataManagement = 8,
    
    /// <summary>Integration features.</summary>
    Integrations = 9
}

/// <summary>
/// Plan comparison for feature comparison tables.
/// </summary>
public record PlanFeatureComparison
{
    /// <summary>
    /// Feature being compared.
    /// </summary>
    public PlanFeature Feature { get; init; }

    /// <summary>
    /// Availability per plan tier (plan name ? enabled/limit).
    /// </summary>
    public Dictionary<string, FeatureAvailability> AvailabilityByPlan { get; init; }

    public PlanFeatureComparison(PlanFeature feature, Dictionary<string, FeatureAvailability> availabilityByPlan)
    {
        Feature = feature;
        AvailabilityByPlan = availabilityByPlan;
    }
}

/// <summary>
/// Feature availability in a specific plan.
/// </summary>
public record FeatureAvailability
{
    /// <summary>
    /// Whether the feature is available.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Limit value if applicable.
    /// </summary>
    public string? LimitDisplay { get; init; }

    /// <summary>
    /// Display text for the availability.
    /// </summary>
    public string DisplayText => IsAvailable
        ? (LimitDisplay ?? "Yes")
        : "No";

    public FeatureAvailability(bool isAvailable, string? limitDisplay = null)
    {
        IsAvailable = isAvailable;
        LimitDisplay = limitDisplay;
    }

    public static FeatureAvailability Available() => new(true);
    public static FeatureAvailability WithLimit(int limit, string unit) => new(true, $"{limit} {unit}");
    public static FeatureAvailability Unlimited() => new(true, "Unlimited");
    public static FeatureAvailability NotAvailable() => new(false);
}
