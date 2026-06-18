using Application.Abstractions.Messaging;

namespace Application.Accounts.UpdateContactPermissions;

/// <summary>
/// Command to update a contact's permissions within an account.
/// Only primary contacts or admins can execute this.
/// </summary>
/// <param name="AccountId">The ID of the account.</param>
/// <param name="ContactId">The ID of the contact to update.</param>
/// <param name="CanCreateTickets">Can create support tickets.</param>
/// <param name="CanViewAllTickets">Can view all account tickets.</param>
/// <param name="CanViewStressData">Can view keyboard stress data.</param>
/// <param name="CanViewReports">Can view stress reports and summaries.</param>
/// <param name="CanViewAnalytics">Can view analytics dashboards.</param>
/// <param name="CanExportData">Can export stress data.</param>
/// <param name="CanManageContacts">Can add/remove contacts.</param>
/// <param name="CanManageSuggestions">Can manage stress relief suggestions.</param>
/// <param name="CanDownloadFiles">Can download report files.</param>
/// <param name="ReceiveNotifications">Receives email notifications.</param>
public sealed record UpdateContactPermissionsCommand(
    Guid AccountId,
    Guid ContactId,
    bool CanCreateTickets,
    bool CanViewAllTickets,
    bool CanViewStressData,
    bool CanViewReports,
    bool CanViewAnalytics,
    bool CanExportData,
    bool CanManageContacts,
    bool CanManageSuggestions,
    bool CanDownloadFiles,
    bool ReceiveNotifications) : ICommand;
