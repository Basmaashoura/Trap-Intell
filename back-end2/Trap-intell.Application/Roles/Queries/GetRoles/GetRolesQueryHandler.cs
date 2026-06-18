using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Queries.GetRoles;

internal sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<PagedResult<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<PagedResult<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var searchTerm = queryOptions.GetSearchTerm();

        IReadOnlyList<Role> roles;

        if (request.OrganizationId.HasValue)
        {
            roles = await _roleRepository.GetRolesForOrganizationAsync(
                request.OrganizationId.Value,
                request.IncludeInactive,
                cancellationToken);
        }
        else
        {
            roles = await _roleRepository.GetSystemRolesAsync(cancellationToken);
        }

        IEnumerable<Role> filteredRoles = roles;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredRoles = filteredRoles.Where(r =>
                r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.Permissions.Any(p => p.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        var sortedRoles = ApplySort(filteredRoles, queryOptions.SortBy, queryOptions.IsSortDescending());
        var totalCount = sortedRoles.Count();

        var pagedRoles = sortedRoles
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = pagedRoles.Select(r => new RoleDto(
            Id: r.Id,
            Name: r.Name,
            Description: r.Description,
            OrganizationId: r.OrganizationId,
            IsSystemRole: r.IsSystemRole,
            IsActive: r.IsActive,
            Permissions: r.Permissions
        )).ToList();

        var result = new PagedResult<RoleDto>(dtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }

    private static IEnumerable<Role> ApplySort(
        IEnumerable<Role> roles,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "name" => descending
                ? roles.OrderByDescending(r => r.Name, StringComparer.OrdinalIgnoreCase)
                : roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase),

            "description" => descending
                ? roles.OrderByDescending(r => r.Description, StringComparer.OrdinalIgnoreCase)
                : roles.OrderBy(r => r.Description, StringComparer.OrdinalIgnoreCase),

            "issystemrole" or "system" => descending
                ? roles.OrderByDescending(r => r.IsSystemRole).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                : roles.OrderBy(r => r.IsSystemRole).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase),

            "isactive" or "active" => descending
                ? roles.OrderByDescending(r => r.IsActive).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                : roles.OrderBy(r => r.IsActive).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase),

            _ => roles
                .OrderByDescending(r => r.IsSystemRole)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
        };
    }
}
