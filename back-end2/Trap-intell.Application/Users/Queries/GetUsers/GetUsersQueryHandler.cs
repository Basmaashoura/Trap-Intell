using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

using UserEntity = Trap_Intel.Domain.Identity.User;

namespace Trap_Intel.Application.Users.Queries.GetUsers;

internal sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var searchTerm = queryOptions.GetSearchTerm();

        var users = await _userRepository.GetByOrganizationAsync(request.OrganizationId, cancellationToken);

        IEnumerable<UserEntity> filteredUsers = users;

        if (request.Status.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.Status == request.Status.Value);
        }

        if (request.RoleId.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.RoleId == request.RoleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredUsers = filteredUsers.Where(u =>
                u.Email.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.FirstName.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var sortedUsers = ApplySort(filteredUsers, queryOptions.SortBy, queryOptions.IsSortDescending());
        var totalCount = sortedUsers.Count();

        var pageItems = sortedUsers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var userDtos = pageItems.Select(u => new UserDto(
            u.Id,
            u.Email.Value,
            u.UserName.Value,
            u.FirstName.Value,
            u.LastName.Value,
            $"{u.FirstName.Value} {u.LastName.Value}",
            u.Status,
            u.RoleId,
            u.OrganizationId,
            u.CreatedAt
        )).ToList();

        var result = new PagedResult<UserDto>(userDtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }

    private static IEnumerable<UserEntity> ApplySort(
        IEnumerable<UserEntity> users,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "createdat" or "created" => descending
                ? users.OrderByDescending(u => u.CreatedAt)
                : users.OrderBy(u => u.CreatedAt),

            "email" => descending
                ? users.OrderByDescending(u => u.Email.Value, StringComparer.OrdinalIgnoreCase)
                : users.OrderBy(u => u.Email.Value, StringComparer.OrdinalIgnoreCase),

            "username" => descending
                ? users.OrderByDescending(u => u.UserName.Value, StringComparer.OrdinalIgnoreCase)
                : users.OrderBy(u => u.UserName.Value, StringComparer.OrdinalIgnoreCase),

            "status" => descending
                ? users.OrderByDescending(u => u.Status).ThenByDescending(u => u.CreatedAt)
                : users.OrderBy(u => u.Status).ThenBy(u => u.CreatedAt),

            _ => descending
                ? users.OrderByDescending(u => u.FirstName.Value, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(u => u.LastName.Value, StringComparer.OrdinalIgnoreCase)
                : users.OrderBy(u => u.FirstName.Value, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(u => u.LastName.Value, StringComparer.OrdinalIgnoreCase)
        };
    }
}
