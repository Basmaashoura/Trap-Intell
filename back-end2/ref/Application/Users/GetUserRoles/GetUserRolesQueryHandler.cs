using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetUserRoles;

/// <summary>
/// Handler for GetUserRolesQuery.
/// Returns all roles assigned to a user.
/// </summary>
internal sealed class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyList<RoleListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetUserRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<RoleListItemResponse>>> Handle(
        GetUserRolesQuery query,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.Id == query.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<IReadOnlyList<RoleListItemResponse>>(UserErrors.NotFound(query.UserId));
        }

        List<RoleListItemResponse> roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == query.UserId)
            .Select(ur => new RoleListItemResponse
            {
                Id = ur.Role!.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsSystemRole = ur.Role.IsSystemRole,
                IsActive = ur.Role.IsActive,
                CreatedAt = ur.Role.CreatedAt,
                UserCount = ur.Role.UserRoles.Count
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RoleListItemResponse>>(roles);
    }
}
