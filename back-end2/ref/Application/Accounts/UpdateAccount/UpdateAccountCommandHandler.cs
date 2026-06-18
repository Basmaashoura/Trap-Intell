using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.UpdateAccount;

/// <summary>
/// Handler for UpdateAccountCommand.
/// Updates an existing account.
/// </summary>
internal sealed class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Check if new name conflicts with existing account (case-insensitive, excluding current)
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
        bool nameExists = await _context.Accounts
            .AnyAsync(a => a.Name.ToUpper() == command.Name.ToUpper() && a.Id != command.AccountId, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        if (nameExists)
        {
            return Result.Failure(AccountErrors.NameNotUnique);
        }

        // Update account using domain method
        account.Update(
            command.Name,
            command.Industry,
            command.Website,
            command.Phone,
            command.Address,
            command.TaxNumber);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
