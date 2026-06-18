using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.GetAccountById;

/// <summary>
/// Handler for GetAccountByIdQuery.
/// Returns detailed account information.
/// </summary>
internal sealed class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountResponse>
{
    private readonly IApplicationDbContext _context;

    public GetAccountByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AccountResponse>> Handle(
        GetAccountByIdQuery query,
        CancellationToken cancellationToken)
    {
        AccountResponse? account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == query.AccountId)
            .Select(a => new AccountResponse
            {
                Id = a.Id,
                Name = a.Name,
                Industry = a.Industry,
                Website = a.Website,
                Phone = a.Phone,
                Address = a.Address,
                TaxNumber = a.TaxNumber,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                ContactCount = a.Contacts.Count(c => c.IsActive && c.IsInviteAccepted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return Result.Failure<AccountResponse>(AccountErrors.NotFound(query.AccountId));
        }

        return Result.Success(account);
    }
}
