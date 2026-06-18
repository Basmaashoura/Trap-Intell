using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Filtering;
using Application.Common.Sorting;
using Application.Extensions;
using Application.Roles.Common;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.GetRoles;

/// <summary>
/// Handler for GetRolesQuery.
/// Returns paginated list of roles with filtering and sorting.
/// </summary>
internal sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, PagedResult<RoleListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<RoleListItemResponse>>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Role> rolesQuery = _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles);

        // Apply search
        rolesQuery = rolesQuery.ApplySearch(RoleSearchConfiguration.Instance, query.SearchTerm);

        // Apply filters
        rolesQuery = ApplyFilters(rolesQuery, query);

        // Apply sorting
        rolesQuery = rolesQuery.ApplySort(RoleSortConfiguration.Instance, query.SortBy, query.SortDirection);

        // Project to response
        IQueryable<RoleListItemResponse> responseQuery = rolesQuery.Select(r => new RoleListItemResponse
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UserCount = r.UserRoles.Count
        });

        // Paginate
        PagedResult<RoleListItemResponse> pagedResult = await responseQuery
            .ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);

        return Result.Success(pagedResult);
    }

    private static IQueryable<Role> ApplyFilters(IQueryable<Role> query, GetRolesQuery request)
    {
        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        if (request.IsSystemRole.HasValue)
        {
            query = query.Where(r => r.IsSystemRole == request.IsSystemRole.Value);
        }

        return query;
    }
}
