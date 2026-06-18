using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using SharedKernel;

namespace Application.Accounts.GetAccounts;

/// <summary>
/// Query to get a paginated list of accounts with filtering and sorting.
/// </summary>
public sealed record GetAccountsQuery : PagedQuery, IQuery<PagedResult<AccountListItemResponse>>
{
    /// <summary>
    /// Search term to filter accounts by name, industry, website, or address.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by active/inactive status.
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Filter by industry.
    /// </summary>
    public string? Industry { get; init; }

    /// <summary>
    /// Sort by field (Name, Industry, CreatedAt, IsActive).
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
