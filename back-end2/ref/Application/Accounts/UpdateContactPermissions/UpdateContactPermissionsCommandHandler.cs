using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.UpdateContactPermissions;

/// <summary>
/// Handler for UpdateContactPermissionsCommand.
/// Only primary contacts or admins can update other contacts' permissions.
/// </summary>
internal sealed class UpdateContactPermissionsCommandHandler : ICommandHandler<UpdateContactPermissionsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateContactPermissionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateContactPermissionsCommand command, CancellationToken cancellationToken)
    {
        // Verify account exists
        bool accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == command.AccountId, cancellationToken);

        if (!accountExists)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Get the contact to update
        AccountContact? contact = await _context.AccountContacts
            .FirstOrDefaultAsync(
                c => c.Id == command.ContactId && c.AccountId == command.AccountId,
                cancellationToken);

        if (contact is null)
        {
            return Result.Failure(AccountContactErrors.ContactNotFound);
        }

        // Cannot update primary contact's permissions (they always have full access)
        if (contact.IsPrimaryContact)
        {
            return Result.Failure(AccountContactErrors.CannotModifyPrimaryContactPermissions);
        }

        // Verify current user has permission to update permissions
        // Must be admin or primary contact of this account
        Guid currentUserId = _currentUserService.UserId;
        
        AccountContact? currentUserContact = await _context.AccountContacts
            .FirstOrDefaultAsync(
                c => c.UserId == currentUserId && 
                     c.AccountId == command.AccountId && 
                     c.IsActive,
                cancellationToken);

        // Allow if user is primary contact with CanManageContacts permission
        bool canManage = currentUserContact is not null &&
                        (currentUserContact.IsPrimaryContact || 
                         currentUserContact.Permissions.CanManageContacts);

        // TODO: Also allow admin users (check roles/permissions)

        if (!canManage)
        {
            return Result.Failure(AccountContactErrors.NotAuthorizedToManageContacts);
        }

        // Create new permissions
        var newPermissions = ContactPermissions.Create(
            command.CanCreateTickets,
            command.CanViewAllTickets,
            command.CanViewStressData,
            command.CanViewReports,
            command.CanViewAnalytics,
            command.CanExportData,
            command.CanManageContacts,
            command.CanManageSuggestions,
            command.CanDownloadFiles,
            command.ReceiveNotifications);

        // Update permissions
        contact.UpdatePermissions(newPermissions);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
