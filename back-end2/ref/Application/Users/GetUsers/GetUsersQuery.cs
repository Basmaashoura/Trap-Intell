using Application.Abstractions.Messaging;
using Domain.Users;
using SharedKernel;

namespace Application.Users.GetUsers;

/// <summary>
/// Query to get a paginated list of users with filtering and sorting.
/// </summary>
public sealed record GetUsersQuery : PagedQuery, IQuery<PagedResult<UserListItemResponse>>
{
    /// <summary>
    /// Search term to filter users by email, first name, or last name.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by user status.
    /// </summary>
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Filter by account type.
    /// </summary>
    public AccountType? AccountType { get; init; }

    /// <summary>
    /// Sort by field (Email, FirstName, LastName, CreatedAt).
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
