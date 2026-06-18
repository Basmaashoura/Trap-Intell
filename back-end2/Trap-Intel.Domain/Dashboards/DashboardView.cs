using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Dashboards.Enums;
using Trap_Intel.Domain.Dashboards.Events;
using Trap_Intel.Domain.Dashboards.ValueObjects;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Dashboards;

/// <summary>
/// Represents a customizable dashboard view for a user.
/// Enables personalization of the platform experience.
/// </summary>
public class DashboardView : AggregateRoot<Guid>
{
    private List<DashboardWidget> _widgets = new();
    private List<Guid> _sharedWithUserIds = new();

    // Private constructor for EF
    private DashboardView() { }

    private DashboardView(
        Guid id,
        Guid userId,
        Guid organizationId,
        string name,
        DashboardType type)
        : base(id)
    {
        UserId = userId;
        OrganizationId = organizationId;
        Name = name;
        Type = type;
        Layout = DashboardLayout.Default();
        IsDefault = false;
        IsShared = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #region Properties

    /// <summary>
    /// User who owns this dashboard.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Organization context.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Dashboard name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Type of dashboard.
    /// </summary>
    public DashboardType Type { get; private set; }

    /// <summary>
    /// Layout configuration.
    /// </summary>
    public DashboardLayout Layout { get; private set; } = null!;

    /// <summary>
    /// Whether this is the user's default dashboard.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Whether this dashboard is shared with others.
    /// </summary>
    public bool IsShared { get; private set; }

    /// <summary>
    /// Auto-refresh interval in seconds (0 = disabled).
    /// </summary>
    public int AutoRefreshSeconds { get; private set; }

    /// <summary>
    /// Default time range for widgets.
    /// </summary>
    public DashboardTimeRange DefaultTimeRange { get; private set; } = DashboardTimeRange.Last24Hours;

    /// <summary>
    /// Theme preference for this dashboard.
    /// </summary>
    public DashboardTheme Theme { get; private set; } = DashboardTheme.System;

    /// <summary>
    /// When dashboard was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When dashboard was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When dashboard was last viewed.
    /// </summary>
    public DateTime? LastViewedAt { get; private set; }

    /// <summary>
    /// View count for analytics.
    /// </summary>
    public int ViewCount { get; private set; }

    /// <summary>
    /// Widgets on this dashboard.
    /// </summary>
    public IReadOnlyList<DashboardWidget> Widgets => _widgets.AsReadOnly();

    /// <summary>
    /// Users this dashboard is shared with.
    /// </summary>
    public IReadOnlyList<Guid> SharedWithUserIds => _sharedWithUserIds.AsReadOnly();

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new dashboard.
    /// </summary>
    public static Result<DashboardView> Create(
        Guid userId,
        Guid organizationId,
        string name,
        DashboardType type = DashboardType.Custom,
        string? description = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure<DashboardView>(DashboardErrors.InvalidUserId);

        if (organizationId == Guid.Empty)
            return Result.Failure<DashboardView>(DashboardErrors.InvalidOrganizationId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<DashboardView>(DashboardErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure<DashboardView>(DashboardErrors.NameTooLong);

        var dashboard = new DashboardView(
            Guid.NewGuid(),
            userId,
            organizationId,
            name.Trim(),
            type)
        {
            Description = description?.Trim()
        };

        dashboard.RaiseDomainEvent(new DashboardCreatedEvent(
            dashboard.Id,
            userId,
            organizationId,
            name,
            type,
            DateTime.UtcNow));

        return Result.Success(dashboard);
    }

    /// <summary>
    /// Create a default dashboard with standard widgets.
    /// </summary>
    public static Result<DashboardView> CreateDefault(
        Guid userId,
        Guid organizationId)
    {
        var result = Create(userId, organizationId, "Overview", DashboardType.Overview, "Default overview dashboard");
        if (result.IsFailure)
            return result;

        var dashboard = result.Value;
        dashboard.IsDefault = true;

        // Add default widgets
        dashboard.AddWidget(DashboardWidget.AttackSummary());
        dashboard.AddWidget(DashboardWidget.ActiveHoneypots());
        dashboard.AddWidget(DashboardWidget.RecentAlerts());
        dashboard.AddWidget(DashboardWidget.ThreatActorsMap());
        dashboard.AddWidget(DashboardWidget.AttackTrend());
        dashboard.AddWidget(DashboardWidget.TopThreatActors());

        return Result.Success(dashboard);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static DashboardView Reconstruct(
        Guid id,
        Guid userId,
        Guid organizationId,
        string name,
        string? description,
        DashboardType type,
        DashboardLayout layout,
        bool isDefault,
        bool isShared,
        int autoRefreshSeconds,
        DashboardTimeRange defaultTimeRange,
        DashboardTheme theme,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? lastViewedAt,
        int viewCount,
        List<DashboardWidget>? widgets = null,
        List<Guid>? sharedWithUserIds = null)
    {
        return new DashboardView
        {
            Id = id,
            UserId = userId,
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            Type = type,
            Layout = layout,
            IsDefault = isDefault,
            IsShared = isShared,
            AutoRefreshSeconds = autoRefreshSeconds,
            DefaultTimeRange = defaultTimeRange,
            Theme = theme,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastViewedAt = lastViewedAt,
            ViewCount = viewCount,
            _widgets = widgets ?? new(),
            _sharedWithUserIds = sharedWithUserIds ?? new()
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Add a widget to the dashboard.
    /// </summary>
    public Result AddWidget(DashboardWidget widget)
    {
        if (widget == null)
            return Result.Failure(DashboardErrors.InvalidWidget);

        if (_widgets.Count >= 20)
            return Result.Failure(DashboardErrors.MaxWidgetsReached);

        if (_widgets.Any(w => w.Id == widget.Id))
            return Result.Failure(DashboardErrors.WidgetAlreadyExists);

        _widgets.Add(widget);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DashboardWidgetAddedEvent(
            Id,
            UserId,
            widget.Id,
            widget.Type,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Remove a widget from the dashboard.
    /// </summary>
    public Result RemoveWidget(Guid widgetId)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget == null)
            return Result.Failure(DashboardErrors.WidgetNotFound);

        _widgets.Remove(widget);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DashboardWidgetRemovedEvent(
            Id,
            UserId,
            widgetId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update widget configuration.
    /// </summary>
    public Result UpdateWidget(Guid widgetId, DashboardWidget updatedWidget)
    {
        var index = _widgets.FindIndex(w => w.Id == widgetId);
        if (index < 0)
            return Result.Failure(DashboardErrors.WidgetNotFound);

        _widgets[index] = updatedWidget with { Id = widgetId };
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Reorder widgets.
    /// </summary>
    public Result ReorderWidgets(List<Guid> widgetIdsInOrder)
    {
        if (widgetIdsInOrder == null || widgetIdsInOrder.Count != _widgets.Count)
            return Result.Failure(DashboardErrors.InvalidWidgetOrder);

        var reordered = new List<DashboardWidget>();
        foreach (var widgetId in widgetIdsInOrder)
        {
            var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
            if (widget == null)
                return Result.Failure(DashboardErrors.WidgetNotFound);
            reordered.Add(widget);
        }

        _widgets = reordered;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update dashboard details.
    /// </summary>
    public Result UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DashboardErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure(DashboardErrors.NameTooLong);

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update layout configuration.
    /// </summary>
    public Result UpdateLayout(DashboardLayout layout)
    {
        if (layout == null)
            return Result.Failure(DashboardErrors.InvalidLayout);

        Layout = layout;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Set as default dashboard.
    /// </summary>
    public void SetAsDefault()
    {
        if (IsDefault) return;

        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DashboardSetAsDefaultEvent(
            Id,
            UserId,
            OrganizationId,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Unset as default dashboard.
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Share dashboard with another user.
    /// </summary>
    public Result ShareWithUser(Guid targetUserId)
    {
        if (targetUserId == Guid.Empty)
            return Result.Failure(DashboardErrors.InvalidUserId);

        if (targetUserId == UserId)
            return Result.Failure(DashboardErrors.CannotShareWithSelf);

        if (_sharedWithUserIds.Contains(targetUserId))
            return Result.Failure(DashboardErrors.AlreadySharedWithUser);

        if (_sharedWithUserIds.Count >= 50)
            return Result.Failure(DashboardErrors.MaxSharesReached);

        _sharedWithUserIds.Add(targetUserId);
        IsShared = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DashboardSharedEvent(
            Id,
            UserId,
            targetUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Unshare dashboard from a user.
    /// </summary>
    public Result UnshareFromUser(Guid targetUserId)
    {
        if (!_sharedWithUserIds.Contains(targetUserId))
            return Result.Failure(DashboardErrors.NotSharedWithUser);

        _sharedWithUserIds.Remove(targetUserId);
        IsShared = _sharedWithUserIds.Count > 0;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DashboardUnsharedEvent(
            Id,
            UserId,
            targetUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update auto-refresh settings.
    /// </summary>
    public Result SetAutoRefresh(int seconds)
    {
        if (seconds < 0)
            return Result.Failure(DashboardErrors.InvalidRefreshInterval);

        if (seconds > 0 && seconds < 30)
            return Result.Failure(DashboardErrors.RefreshIntervalTooShort);

        AutoRefreshSeconds = seconds;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update default time range.
    /// </summary>
    public void SetDefaultTimeRange(DashboardTimeRange timeRange)
    {
        DefaultTimeRange = timeRange;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update theme preference.
    /// </summary>
    public void SetTheme(DashboardTheme theme)
    {
        Theme = theme;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record that dashboard was viewed.
    /// </summary>
    public void RecordView()
    {
        LastViewedAt = DateTime.UtcNow;
        ViewCount++;
    }

    /// <summary>
    /// Duplicate this dashboard for another user (or same user).
    /// </summary>
    public Result<DashboardView> Duplicate(Guid targetUserId, string newName)
    {
        var result = Create(targetUserId, OrganizationId, newName, Type, Description);
        if (result.IsFailure)
            return result;

        var duplicate = result.Value;
        duplicate.Layout = Layout;
        duplicate.AutoRefreshSeconds = AutoRefreshSeconds;
        duplicate.DefaultTimeRange = DefaultTimeRange;
        duplicate.Theme = Theme;
        duplicate._widgets = _widgets.Select(w => w with { Id = Guid.NewGuid() }).ToList();

        duplicate.RaiseDomainEvent(new DashboardDuplicatedEvent(
            duplicate.Id,
            Id,
            targetUserId,
            DateTime.UtcNow));

        return Result.Success(duplicate);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if user can view this dashboard.
    /// </summary>
    public bool CanView(Guid requestingUserId)
    {
        return UserId == requestingUserId || _sharedWithUserIds.Contains(requestingUserId);
    }

    /// <summary>
    /// Check if user can edit this dashboard.
    /// </summary>
    public bool CanEdit(Guid requestingUserId)
    {
        return UserId == requestingUserId;
    }

    /// <summary>
    /// Get widget by ID.
    /// </summary>
    public DashboardWidget? GetWidget(Guid widgetId)
    {
        return _widgets.FirstOrDefault(w => w.Id == widgetId);
    }

    /// <summary>
    /// Get widgets by type.
    /// </summary>
    public IEnumerable<DashboardWidget> GetWidgetsByType(WidgetType type)
    {
        return _widgets.Where(w => w.Type == type);
    }

    #endregion
}
