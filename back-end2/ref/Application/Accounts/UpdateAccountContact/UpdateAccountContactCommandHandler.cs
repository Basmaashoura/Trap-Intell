using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.UpdateAccountContact;

/// <summary>
/// Handler for UpdateAccountContactCommand.
/// Updates an account contact's information.
/// </summary>
internal sealed class UpdateAccountContactCommandHandler : ICommandHandler<UpdateAccountContactCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAccountContactCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateAccountContactCommand command, CancellationToken cancellationToken)
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
            return Result.Failure(AccountContactErrors.NotFound(command.ContactId));
        }

        // If setting as primary, remove primary from other contacts
        if (command.IsPrimaryContact && !contact.IsPrimaryContact)
        {
            foreach (AccountContact existingContact in account.Contacts.Where(c => c.IsPrimaryContact && c.Id != command.ContactId))
            {
                existingContact.RemoveAsPrimary();
            }
            contact.SetAsPrimary();
        }
        else if (!command.IsPrimaryContact && contact.IsPrimaryContact)
        {
            // Ensure at least one primary contact exists
            bool hasOtherPrimary = account.Contacts.Exists(c => c.IsPrimaryContact && c.Id != command.ContactId);
            if (!hasOtherPrimary)
            {
                return Result.Failure(AccountContactErrors.AccountMustHavePrimaryContact);
            }
            contact.RemoveAsPrimary();
        }

        // Update role and decision maker status
        contact.UpdateRole(command.Role, command.IsDecisionMaker);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
