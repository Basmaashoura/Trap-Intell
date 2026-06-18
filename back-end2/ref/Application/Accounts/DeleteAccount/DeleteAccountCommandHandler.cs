using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.DeleteAccount;

/// <summary>
/// Handler for DeleteAccountCommand.
/// Permanently deletes an account.
/// </summary>
internal sealed class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Cannot delete accounts with active contacts
        if (account.Contacts.Exists(c => c.IsActive))
        {
            return Result.Failure(AccountErrors.HasActiveContacts);
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
