using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.GetRolePermissions;

/// <summary>
/// Handler for GetRolePermissionsQuery.
/// Returns all permissions assigned to a role.
/// </summary>
internal sealed class GetRolePermissionsQueryHandler : IQueryHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetRolePermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(
        GetRolePermissionsQuery query,
        CancellationToken cancellationToken)
    {
        // Check if role exists
        bool roleExists = await _context.Roles
            .AnyAsync(r => r.Id == query.RoleId, cancellationToken);

        if (!roleExists)
        {
            return Result.Failure<IReadOnlyList<PermissionResponse>>(RoleErrors.NotFound(query.RoleId));
        }

        List<PermissionResponse> permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == query.RoleId)
            .Select(rp => new PermissionResponse
            {
                Id = rp.Permission!.Id,
                Code = rp.Permission.Code,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Module = rp.Permission.Module.ToString(),
                Action = rp.Permission.Action.ToString()
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PermissionResponse>>(permissions);
    }
}
