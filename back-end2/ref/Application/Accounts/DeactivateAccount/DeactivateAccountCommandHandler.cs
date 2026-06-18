using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.DeactivateAccount;

/// <summary>
/// Handler for DeactivateAccountCommand.
/// Deactivates an account (soft delete).
/// </summary>
internal sealed class DeactivateAccountCommandHandler : ICommandHandler<DeactivateAccountCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeactivateAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Domain method handles already inactive check and raises event
        account.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
