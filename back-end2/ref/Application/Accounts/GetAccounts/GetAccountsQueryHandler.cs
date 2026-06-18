using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Application.Common.Filtering;
using Application.Common.Sorting;
using Application.Extensions;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.GetAccounts;

/// <summary>
/// Handler for GetAccountsQuery.
/// Returns paginated list of accounts with filtering and sorting.
/// </summary>
internal sealed class GetAccountsQueryHandler : IQueryHandler<GetAccountsQuery, PagedResult<AccountListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AccountListItemResponse>>> Handle(
        GetAccountsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Account> accountsQuery = _context.Accounts
            .AsNoTracking()
            .Include(a => a.Contacts);

        // Apply search
        accountsQuery = accountsQuery.ApplySearch(AccountSearchConfiguration.Instance, query.SearchTerm);

        // Apply filters
        accountsQuery = ApplyFilters(accountsQuery, query);

        // Apply sorting
        accountsQuery = accountsQuery.ApplySort(AccountSortConfiguration.Instance, query.SortBy, query.SortDirection);

        // Project to response
        IQueryable<AccountListItemResponse> responseQuery = accountsQuery.Select(a => new AccountListItemResponse
        {
            Id = a.Id,
            Name = a.Name,
            Industry = a.Industry,
            Website = a.Website,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            ContactCount = a.Contacts.Count(c => c.IsActive && c.IsInviteAccepted)
        });

        // Paginate
        PagedResult<AccountListItemResponse> pagedResult = await responseQuery
            .ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);

        return Result.Success(pagedResult);
    }

    private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, GetAccountsQuery request)
    {
        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
            query = query.Where(a => a.Industry != null && a.Industry.ToUpper() == request.Industry.ToUpper());
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query;
    }
}
