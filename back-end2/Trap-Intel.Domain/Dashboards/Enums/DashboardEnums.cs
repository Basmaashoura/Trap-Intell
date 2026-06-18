namespace Trap_Intel.Domain.Dashboards.Enums;

/// <summary>
/// Type of dashboard.
/// </summary>
public enum DashboardType
{
    /// <summary>Overview/summary dashboard.</summary>
    Overview = 0,
    
    /// <summary>Security operations center dashboard.</summary>
    SOC = 1,
    
    /// <summary>Executive/management dashboard.</summary>
    Executive = 2,
    
    /// <summary>Honeypot monitoring dashboard.</summary>
    Honeypots = 3,
    
    /// <summary>Threat intelligence dashboard.</summary>
    ThreatIntel = 4,
    
    /// <summary>Alerts management dashboard.</summary>
    Alerts = 5,
    
    /// <summary>Attack analysis dashboard.</summary>
    Attacks = 6,
    
    /// <summary>Custom user-created dashboard.</summary>
    Custom = 7
}

/// <summary>
/// Type of widget on a dashboard.
/// </summary>
public enum WidgetType
{
    // Summary Widgets
    AttackSummary = 0,
    AlertSummary = 1,
    HoneypotSummary = 2,
    ThreatActorSummary = 3,
    
    // Chart Widgets
    AttackTrendChart = 10,
    AttackTypeChart = 11,
    SeverityDistributionChart = 12,
    GeographicDistributionChart = 13,
    TimelineChart = 14,
    
    // List Widgets
    RecentAlertsList = 20,
    RecentAttacksList = 21,
    TopThreatActorsList = 22,
    ActiveHoneypotsList = 23,
    RecommendationsList = 24,
    
    // Map Widgets
    AttackOriginMap = 30,
    ThreatActorMap = 31,
    HoneypotLocationMap = 32,
    
    // Metric Widgets
    SingleMetric = 40,
    MetricComparison = 41,
    MetricTrend = 42,
    
    // Status Widgets
    SystemStatus = 50,
    HoneypotHealth = 51,
    QuotaUsage = 52,
    
    // Table Widgets
    DataTable = 60,
    
    // Custom Widgets
    Custom = 99
}

/// <summary>
/// Dashboard time range for filtering data.
/// </summary>
public enum DashboardTimeRange
{
    /// <summary>Last hour.</summary>
    LastHour = 0,
    
    /// <summary>Last 4 hours.</summary>
    Last4Hours = 1,
    
    /// <summary>Last 24 hours.</summary>
    Last24Hours = 2,
    
    /// <summary>Last 7 days.</summary>
    Last7Days = 3,
    
    /// <summary>Last 30 days.</summary>
    Last30Days = 4,
    
    /// <summary>Last 90 days.</summary>
    Last90Days = 5,
    
    /// <summary>This month.</summary>
    ThisMonth = 6,
    
    /// <summary>Last month.</summary>
    LastMonth = 7,
    
    /// <summary>This year.</summary>
    ThisYear = 8,
    
    /// <summary>Custom date range.</summary>
    Custom = 9
}

/// <summary>
/// Dashboard theme preference.
/// </summary>
public enum DashboardTheme
{
    /// <summary>Follow system preference.</summary>
    System = 0,
    
    /// <summary>Light theme.</summary>
    Light = 1,
    
    /// <summary>Dark theme.</summary>
    Dark = 2,
    
    /// <summary>High contrast theme.</summary>
    HighContrast = 3
}

/// <summary>
/// Widget size for grid layout.
/// </summary>
public enum WidgetSize
{
    /// <summary>Small (1x1 grid cells).</summary>
    Small = 0,
    
    /// <summary>Medium (2x1 grid cells).</summary>
    Medium = 1,
    
    /// <summary>Large (2x2 grid cells).</summary>
    Large = 2,
    
    /// <summary>Wide (4x1 grid cells).</summary>
    Wide = 3,
    
    /// <summary>Tall (1x2 grid cells).</summary>
    Tall = 4,
    
    /// <summary>Full width (full row).</summary>
    FullWidth = 5
}

/// <summary>
/// Layout type for dashboard.
/// </summary>
public enum LayoutType
{
    /// <summary>Responsive grid layout.</summary>
    Grid = 0,
    
    /// <summary>Fixed column layout.</summary>
    Columns = 1,
    
    /// <summary>Free-form layout.</summary>
    Freeform = 2
}
