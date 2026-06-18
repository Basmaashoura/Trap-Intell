namespace Domain.Accounts;

/// <summary>
/// Defines what an end user contact can see and do within the portal.
/// This allows the primary contact or admin to control other contacts' access.
/// </summary>
public sealed class ContactPermissions
{
    /// <summary>
    /// Can create new support tickets.
    /// </summary>
    public bool CanCreateTickets { get; private set; }

    /// <summary>
    /// Can view all tickets for the account (not just their own).
    /// </summary>
    public bool CanViewAllTickets { get; private set; }

    /// <summary>
    /// Can view keyboard stress data and readings.
    /// </summary>
    public bool CanViewStressData { get; private set; }

    /// <summary>
    /// Can view stress analysis reports and summaries.
    /// </summary>
    public bool CanViewReports { get; private set; }

    /// <summary>
    /// Can view analytics dashboards and trends.
    /// </summary>
    public bool CanViewAnalytics { get; private set; }

    /// <summary>
    /// Can export stress data and reports.
    /// </summary>
    public bool CanExportData { get; private set; }

    /// <summary>
    /// Can add new contacts to the account (within limit).
    /// </summary>
    public bool CanManageContacts { get; private set; }

    /// <summary>
    /// Can manage stress relief suggestions and preferences.
    /// </summary>
    public bool CanManageSuggestions { get; private set; }

    /// <summary>
    /// Can download report files and exported data.
    /// </summary>
    public bool CanDownloadFiles { get; private set; }

    /// <summary>
    /// Can receive email notifications.
    /// </summary>
    public bool ReceiveNotifications { get; private set; }

    private ContactPermissions()
    {
    }

    /// <summary>
    /// Creates minimal permissions for a new contact (view-only, no actions).
    /// Primary contact should set proper permissions after invitation is accepted.
    /// </summary>
    public static ContactPermissions CreateMinimal()
    {
        return new ContactPermissions
        {
            CanCreateTickets = false,
            CanViewAllTickets = false,
            CanViewStressData = false,
            CanViewReports = false,
            CanViewAnalytics = false,
            CanExportData = false,
            CanManageContacts = false,
            CanManageSuggestions = false,
            CanDownloadFiles = false,
            ReceiveNotifications = true
        };
    }

    /// <summary>
    /// Creates default permissions for a new contact (basic access).
    /// </summary>
    public static ContactPermissions CreateDefault()
    {
        return new ContactPermissions
        {
            CanCreateTickets = true,
            CanViewAllTickets = false,
            CanViewStressData = true,
            CanViewReports = false,
            CanViewAnalytics = false,
            CanExportData = false,
            CanManageContacts = false,
            CanManageSuggestions = false,
            CanDownloadFiles = true,
            ReceiveNotifications = true
        };
    }

    /// <summary>
    /// Creates full permissions for primary contact / decision maker.
    /// </summary>
    public static ContactPermissions CreateFull()
    {
        return new ContactPermissions
        {
            CanCreateTickets = true,
            CanViewAllTickets = true,
            CanViewStressData = true,
            CanViewReports = true,
            CanViewAnalytics = true,
            CanExportData = true,
            CanManageContacts = true,
            CanManageSuggestions = true,
            CanDownloadFiles = true,
            ReceiveNotifications = true
        };
    }

    /// <summary>
    /// Creates custom permissions.
    /// </summary>
    public static ContactPermissions Create(
        bool canCreateTickets,
        bool canViewAllTickets,
        bool canViewStressData,
        bool canViewReports,
        bool canViewAnalytics,
        bool canExportData,
        bool canManageContacts,
        bool canManageSuggestions,
        bool canDownloadFiles,
        bool receiveNotifications)
    {
        return new ContactPermissions
        {
            CanCreateTickets = canCreateTickets,
            CanViewAllTickets = canViewAllTickets,
            CanViewStressData = canViewStressData,
            CanViewReports = canViewReports,
            CanViewAnalytics = canViewAnalytics,
            CanExportData = canExportData,
            CanManageContacts = canManageContacts,
            CanManageSuggestions = canManageSuggestions,
            CanDownloadFiles = canDownloadFiles,
            ReceiveNotifications = receiveNotifications
        };
    }

    /// <summary>
    /// Updates specific permissions.
    /// </summary>
    public ContactPermissions WithTicketPermissions(bool canCreate, bool canViewAll)
    {
        return new ContactPermissions
        {
            CanCreateTickets = canCreate,
            CanViewAllTickets = canViewAll,
            CanViewStressData = CanViewStressData,
            CanViewReports = CanViewReports,
            CanViewAnalytics = CanViewAnalytics,
            CanExportData = CanExportData,
            CanManageContacts = CanManageContacts,
            CanManageSuggestions = CanManageSuggestions,
            CanDownloadFiles = CanDownloadFiles,
            ReceiveNotifications = ReceiveNotifications
        };
    }

    /// <summary>
    /// Updates data access permissions.
    /// </summary>
    public ContactPermissions WithDataPermissions(bool canViewReports, bool canViewAnalytics, bool canExportData)
    {
        return new ContactPermissions
        {
            CanCreateTickets = CanCreateTickets,
            CanViewAllTickets = CanViewAllTickets,
            CanViewStressData = CanViewStressData,
            CanViewReports = canViewReports,
            CanViewAnalytics = canViewAnalytics,
            CanExportData = canExportData,
            CanManageContacts = CanManageContacts,
            CanManageSuggestions = CanManageSuggestions,
            CanDownloadFiles = CanDownloadFiles,
            ReceiveNotifications = ReceiveNotifications
        };
    }
}
