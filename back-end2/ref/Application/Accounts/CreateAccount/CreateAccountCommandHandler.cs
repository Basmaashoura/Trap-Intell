using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.CreateAccount;

/// <summary>
/// Handler for CreateAccountCommand.
/// Creates a new account in the system.
/// </summary>
internal sealed class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        // Check if account name already exists (case-insensitive)
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
        bool nameExists = await _context.Accounts
            .AnyAsync(a => a.Name.ToUpper() == command.Name.ToUpper(), cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        if (nameExists)
        {
            return Result.Failure<Guid>(AccountErrors.NameNotUnique);
        }

        // Create new account (Domain method raises AccountCreatedDomainEvent)
        var account = Account.Create(
            command.Name,
            command.Industry,
            command.Website,
            command.Phone,
            command.Address,
            command.TaxNumber);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(account.Id);
    }
}
