using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Filtering;
using Application.Common.Sorting;
using Application.Extensions;
using Application.Users.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetUsers;

/// <summary>
/// Handler for GetUsersQuery.
/// Returns paginated list of users with filtering and sorting.
/// </summary>
internal sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<UserListItemResponse>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        // Start with base query
        IQueryable<User> usersQuery = _context.Users.AsNoTracking();

        // Apply search (using reusable configuration)
        usersQuery = usersQuery.ApplySearch(UserSearchConfiguration.Instance, query.SearchTerm);

        // Apply filters
        usersQuery = ApplyFilters(usersQuery, query);

        // Apply sorting (using reusable configuration)
        usersQuery = usersQuery.ApplySort(UserSortConfiguration.Instance, query.SortBy, query.SortDirection);

        // Project to response
        IQueryable<UserListItemResponse> responseQuery = usersQuery.Select(u => new UserListItemResponse
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            AccountType = u.AccountType,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        });

        // Paginate
        PagedResult<UserListItemResponse> pagedResult = await responseQuery
            .ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);

        return Result.Success(pagedResult);
    }

    private static IQueryable<User> ApplyFilters(IQueryable<User> query, GetUsersQuery request)
    {
        // Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        // Account type filter
        if (request.AccountType.HasValue)
        {
            query = query.Where(u => u.AccountType == request.AccountType.Value);
        }

        return query;
    }
}
