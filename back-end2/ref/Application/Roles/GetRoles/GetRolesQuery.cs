using Application.Abstractions.Messaging;
using Application.Roles.Common;
using SharedKernel;

namespace Application.Roles.GetRoles;

/// <summary>
/// Query to get a paginated list of roles with filtering and sorting.
/// </summary>
public sealed record GetRolesQuery : PagedQuery, IQuery<PagedResult<RoleListItemResponse>>
{
    /// <summary>
    /// Search term to filter roles by name or description.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by active/inactive status.
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Filter by system role status.
    /// </summary>
    public bool? IsSystemRole { get; init; }

    /// <summary>
    /// Sort by field (Name, CreatedAt, IsActive, IsSystemRole).
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
