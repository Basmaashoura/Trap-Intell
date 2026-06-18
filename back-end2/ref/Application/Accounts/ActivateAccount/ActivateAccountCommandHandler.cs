using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.ActivateAccount;

/// <summary>
/// Handler for ActivateAccountCommand.
/// Activates a deactivated account.
/// </summary>
internal sealed class ActivateAccountCommandHandler : ICommandHandler<ActivateAccountCommand>
{
    private readonly IApplicationDbContext _context;

    public ActivateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        account.Activate();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
