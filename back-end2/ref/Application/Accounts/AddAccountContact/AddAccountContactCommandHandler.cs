using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.AddAccountContact;

/// <summary>
/// Handler for AddAccountContactCommand.
/// Adds a contact to an account.
/// </summary>
internal sealed class AddAccountContactCommandHandler : ICommandHandler<AddAccountContactCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddAccountContactCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(AddAccountContactCommand command, CancellationToken cancellationToken)
    {
        // Check if account exists
        Account? account = await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(AccountErrors.NotFound(command.AccountId));
        }

        // Check if user exists and is an EndUser type
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
        }

        if (user.AccountType != AccountType.EndUser)
        {
            return Result.Failure<Guid>(AccountContactErrors.UserNotEndUser);
        }

        // Check if user is already a contact of this account
        if (account.HasContact(command.UserId))
        {
            return Result.Failure<Guid>(AccountContactErrors.AlreadyContact);
        }

        // If setting as primary, remove primary from existing contacts
        if (command.IsPrimaryContact)
        {
            foreach (AccountContact existingContact in account.Contacts.Where(c => c.IsPrimaryContact))
            {
                existingContact.RemoveAsPrimary();
            }
        }

        // Create contact using domain factory method
        var contact = AccountContact.CreateDirect(
            command.AccountId,
            command.UserId,
            command.IsPrimaryContact,
            command.Role,
            command.IsDecisionMaker);

        _context.AccountContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(contact.Id);
    }
}
