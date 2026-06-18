using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.GetAccountContacts;

/// <summary>
/// Handler for GetAccountContactsQuery.
/// Returns all contacts for an account.
/// </summary>
internal sealed class GetAccountContactsQueryHandler : IQueryHandler<GetAccountContactsQuery, IReadOnlyList<AccountContactResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountContactsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<AccountContactResponse>>> Handle(
        GetAccountContactsQuery query,
        CancellationToken cancellationToken)
    {
        // Check if account exists
        bool accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == query.AccountId, cancellationToken);

        if (!accountExists)
        {
            return Result.Failure<IReadOnlyList<AccountContactResponse>>(AccountErrors.NotFound(query.AccountId));
        }

        List<AccountContactResponse> contacts = await _context.AccountContacts
            .AsNoTracking()
            .Where(c => c.AccountId == query.AccountId)
            .Select(c => new AccountContactResponse
            {
                Id = c.Id,
                UserId = c.UserId,
                Email = c.User!.Email,
                FirstName = c.User.FirstName,
                LastName = c.User.LastName,
                Role = c.Role,
                IsPrimaryContact = c.IsPrimaryContact,
                IsDecisionMaker = c.IsDecisionMaker,
                IsActive = c.IsActive,
                IsInviteAccepted = c.IsInviteAccepted,
                CreatedAt = c.CreatedAt
            })
            .OrderByDescending(c => c.IsPrimaryContact)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AccountContactResponse>>(contacts);
    }
}
