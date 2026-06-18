using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.RemoveAccountContact;

/// <summary>
/// Handler for RemoveAccountContactCommand.
/// Removes a contact from an account (soft delete).
/// </summary>
internal sealed class RemoveAccountContactCommandHandler : ICommandHandler<RemoveAccountContactCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveAccountContactCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RemoveAccountContactCommand command, CancellationToken cancellationToken)
    {
        // Check if account exists
        Account? account = await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Find the contact
        AccountContact? contact = account.Contacts.Find(c => c.Id == command.ContactId);

        if (contact is null)
        {
            return Result.Success(); // Already removed or doesn't exist
        }

        // Cannot remove primary contact without assigning another one
        if (contact.IsPrimaryContact)
        {
            bool hasOtherActiveContacts = account.Contacts.Exists(c => c.IsActive && c.Id != command.ContactId);
            if (hasOtherActiveContacts)
            {
                return Result.Failure(AccountContactErrors.CannotRemovePrimaryContact);
            }
        }

        // Deactivate the contact (soft delete)
        contact.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
