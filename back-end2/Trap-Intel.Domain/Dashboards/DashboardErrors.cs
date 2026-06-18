using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Dashboards;

public static class DashboardErrors
{
    #region Validation Errors

    public static readonly Error InvalidUserId = Error.Custom(
        "Dashboard.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "Dashboard.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidName = Error.Custom(
        "Dashboard.InvalidName",
        "Dashboard name cannot be empty");

    public static readonly Error NameTooLong = Error.Custom(
        "Dashboard.NameTooLong",
        "Dashboard name cannot exceed 100 characters");

    public static readonly Error InvalidWidget = Error.Custom(
        "Dashboard.InvalidWidget",
        "Widget cannot be null");

    public static readonly Error InvalidLayout = Error.Custom(
        "Dashboard.InvalidLayout",
        "Layout cannot be null");

    public static readonly Error InvalidWidgetOrder = Error.Custom(
        "Dashboard.InvalidWidgetOrder",
        "Widget order list is invalid");

    public static readonly Error InvalidRefreshInterval = Error.Custom(
        "Dashboard.InvalidRefreshInterval",
        "Refresh interval cannot be negative");

    public static readonly Error RefreshIntervalTooShort = Error.Custom(
        "Dashboard.RefreshIntervalTooShort",
        "Refresh interval must be at least 30 seconds");

    #endregion

    #region Widget Errors

    public static readonly Error WidgetNotFound = Error.Custom(
        "Dashboard.WidgetNotFound",
        "Widget not found");

    public static readonly Error WidgetAlreadyExists = Error.Custom(
        "Dashboard.WidgetAlreadyExists",
        "Widget already exists on this dashboard");

    public static readonly Error MaxWidgetsReached = Error.Custom(
        "Dashboard.MaxWidgetsReached",
        "Maximum number of widgets (20) reached");

    #endregion

    #region Sharing Errors

    public static readonly Error CannotShareWithSelf = Error.Custom(
        "Dashboard.CannotShareWithSelf",
        "Cannot share dashboard with yourself");

    public static readonly Error AlreadySharedWithUser = Error.Custom(
        "Dashboard.AlreadySharedWithUser",
        "Dashboard is already shared with this user");

    public static readonly Error NotSharedWithUser = Error.Custom(
        "Dashboard.NotSharedWithUser",
        "Dashboard is not shared with this user");

    public static readonly Error MaxSharesReached = Error.Custom(
        "Dashboard.MaxSharesReached",
        "Maximum number of shares (50) reached");

    #endregion

    #region Access Errors

    public static readonly Error DashboardNotFound = Error.Custom(
        "Dashboard.NotFound",
        "Dashboard not found");

    public static readonly Error AccessDenied = Error.Custom(
        "Dashboard.AccessDenied",
        "You don't have access to this dashboard");

    public static readonly Error EditDenied = Error.Custom(
        "Dashboard.EditDenied",
        "You cannot edit this dashboard");

    #endregion

    #region Quota Errors

    public static readonly Error MaxDashboardsReached = Error.Custom(
        "Dashboard.MaxDashboardsReached",
        "Maximum number of dashboards reached");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid dashboardId) => Error.Custom(
        "Dashboard.NotFound",
        $"Dashboard with ID '{dashboardId}' not found");

    #endregion
}
