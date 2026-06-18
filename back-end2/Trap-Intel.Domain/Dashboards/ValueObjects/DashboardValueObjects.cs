using Trap_Intel.Domain.Dashboards.Enums;

namespace Trap_Intel.Domain.Dashboards.ValueObjects;

/// <summary>
/// Represents a widget on a dashboard.
/// </summary>
public record DashboardWidget
{
    /// <summary>
    /// Unique widget ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Widget type.
    /// </summary>
    public WidgetType Type { get; init; }

    /// <summary>
    /// Widget title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Widget size.
    /// </summary>
    public WidgetSize Size { get; init; }

    /// <summary>
    /// Position in grid (row).
    /// </summary>
    public int Row { get; init; }

    /// <summary>
    /// Position in grid (column).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Configuration JSON.
    /// </summary>
    public string? Configuration { get; init; }

    /// <summary>
    /// Data source/filter.
    /// </summary>
    public WidgetDataSource? DataSource { get; init; }

    /// <summary>
    /// Whether widget is visible.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Auto-refresh interval override (seconds).
    /// </summary>
    public int? RefreshIntervalOverride { get; init; }

    public DashboardWidget(
        Guid id,
        WidgetType type,
        string title,
        WidgetSize size = WidgetSize.Medium,
        int row = 0,
        int column = 0,
        string? configuration = null,
        WidgetDataSource? dataSource = null)
    {
        Id = id;
        Type = type;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Size = size;
        Row = row;
        Column = column;
        Configuration = configuration;
        DataSource = dataSource;
    }

    #region Factory Methods

    public static DashboardWidget AttackSummary(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.AttackSummary,
        "Attack Summary",
        WidgetSize.Medium,
        row, column);

    public static DashboardWidget AlertSummary(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.AlertSummary,
        "Alert Summary",
        WidgetSize.Medium,
        row, column);

    public static DashboardWidget ActiveHoneypots(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.HoneypotSummary,
        "Active Honeypots",
        WidgetSize.Small,
        row, column);

    public static DashboardWidget RecentAlerts(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.RecentAlertsList,
        "Recent Alerts",
        WidgetSize.Large,
        row, column);

    public static DashboardWidget ThreatActorsMap(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.ThreatActorMap,
        "Threat Actor Origins",
        WidgetSize.Large,
        row, column);

    public static DashboardWidget AttackTrend(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.AttackTrendChart,
        "Attack Trend",
        WidgetSize.Wide,
        row, column);

    public static DashboardWidget TopThreatActors(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.TopThreatActorsList,
        "Top Threat Actors",
        WidgetSize.Medium,
        row, column);

    public static DashboardWidget SeverityDistribution(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.SeverityDistributionChart,
        "Severity Distribution",
        WidgetSize.Medium,
        row, column);

    public static DashboardWidget HoneypotHealth(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.HoneypotHealth,
        "Honeypot Health",
        WidgetSize.Medium,
        row, column);

    public static DashboardWidget QuotaUsage(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.QuotaUsage,
        "Quota Usage",
        WidgetSize.Small,
        row, column);

    public static DashboardWidget Metric(string title, string metricKey, int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.SingleMetric,
        title,
        WidgetSize.Small,
        row, column,
        $"{{\"metricKey\": \"{metricKey}\"}}");

    public static DashboardWidget Recommendations(int row = 0, int column = 0) => new(
        Guid.NewGuid(),
        WidgetType.RecommendationsList,
        "AI Recommendations",
        WidgetSize.Large,
        row, column);

    #endregion
}

/// <summary>
/// Data source configuration for a widget.
/// </summary>
public record WidgetDataSource
{
    /// <summary>
    /// Source type (e.g., "attacks", "alerts", "honeypots").
    /// </summary>
    public string SourceType { get; init; }

    /// <summary>
    /// Filter expression (JSON).
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Maximum items to display.
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// Sort field.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction.
    /// </summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Time range override.
    /// </summary>
    public DashboardTimeRange? TimeRangeOverride { get; init; }

    public WidgetDataSource(
        string sourceType,
        string? filter = null,
        int? maxItems = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        Filter = filter;
        MaxItems = maxItems;
        SortBy = sortBy;
        SortDescending = sortDescending;
    }

    public static WidgetDataSource Attacks(int? maxItems = null) =>
        new("attacks", maxItems: maxItems, sortBy: "timestamp");

    public static WidgetDataSource Alerts(int? maxItems = null) =>
        new("alerts", maxItems: maxItems, sortBy: "createdAt");

    public static WidgetDataSource ThreatActors(int? maxItems = null) =>
        new("threatActors", maxItems: maxItems, sortBy: "threatScore");

    public static WidgetDataSource Honeypots() =>
        new("honeypots", sortBy: "lastHeartbeat");
}

/// <summary>
/// Dashboard layout configuration.
/// </summary>
public record DashboardLayout
{
    /// <summary>
    /// Layout type.
    /// </summary>
    public LayoutType Type { get; init; }

    /// <summary>
    /// Number of columns in grid.
    /// </summary>
    public int Columns { get; init; }

    /// <summary>
    /// Row height in pixels.
    /// </summary>
    public int RowHeight { get; init; }

    /// <summary>
    /// Gap between widgets in pixels.
    /// </summary>
    public int Gap { get; init; }

    /// <summary>
    /// Padding around dashboard in pixels.
    /// </summary>
    public int Padding { get; init; }

    /// <summary>
    /// Whether widgets can be dragged.
    /// </summary>
    public bool IsDraggable { get; init; }

    /// <summary>
    /// Whether widgets can be resized.
    /// </summary>
    public bool IsResizable { get; init; }

    public DashboardLayout(
        LayoutType type = LayoutType.Grid,
        int columns = 4,
        int rowHeight = 100,
        int gap = 16,
        int padding = 24,
        bool isDraggable = true,
        bool isResizable = true)
    {
        Type = type;
        Columns = columns > 0 ? columns : 4;
        RowHeight = rowHeight > 0 ? rowHeight : 100;
        Gap = gap >= 0 ? gap : 16;
        Padding = padding >= 0 ? padding : 24;
        IsDraggable = isDraggable;
        IsResizable = isResizable;
    }

    public static DashboardLayout Default() => new();

    public static DashboardLayout Compact() => new(
        columns: 6,
        rowHeight: 80,
        gap: 8,
        padding: 16);

    public static DashboardLayout Wide() => new(
        columns: 3,
        rowHeight: 120,
        gap: 24,
        padding: 32);

    public static DashboardLayout Fixed() => new(
        type: LayoutType.Columns,
        columns: 2,
        isDraggable: false,
        isResizable: false);
}

/// <summary>
/// Dashboard display information (safe for listing).
/// </summary>
public record DashboardDisplayInfo
{
    /// <summary>
    /// Dashboard ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Dashboard name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Dashboard type.
    /// </summary>
    public DashboardType Type { get; init; }

    /// <summary>
    /// Whether this is the default dashboard.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Whether this is a shared dashboard.
    /// </summary>
    public bool IsShared { get; init; }

    /// <summary>
    /// Whether user owns this dashboard.
    /// </summary>
    public bool IsOwned { get; init; }

    /// <summary>
    /// Number of widgets.
    /// </summary>
    public int WidgetCount { get; init; }

    /// <summary>
    /// Last viewed timestamp.
    /// </summary>
    public DateTime? LastViewedAt { get; init; }

    /// <summary>
    /// Owner name (if shared).
    /// </summary>
    public string? OwnerName { get; init; }

    public DashboardDisplayInfo(
        Guid id,
        string name,
        string? description,
        DashboardType type,
        bool isDefault,
        bool isShared,
        bool isOwned,
        int widgetCount,
        DateTime? lastViewedAt,
        string? ownerName = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        IsDefault = isDefault;
        IsShared = isShared;
        IsOwned = isOwned;
        WidgetCount = widgetCount;
        LastViewedAt = lastViewedAt;
        OwnerName = ownerName;
    }
}
