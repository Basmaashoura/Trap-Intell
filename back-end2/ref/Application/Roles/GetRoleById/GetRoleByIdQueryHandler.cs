using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.GetRoleById;

/// <summary>
/// Handler for GetRoleByIdQuery.
/// Returns detailed role information including permission and user counts.
/// </summary>
internal sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    private readonly IApplicationDbContext _context;

    public GetRoleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery query,
        CancellationToken cancellationToken)
    {
        RoleResponse? role = await _context.Roles
            .AsNoTracking()
            .Where(r => r.Id == query.RoleId)
            .Select(r => new RoleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                CanViewSensitiveData = r.CanViewSensitiveData,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                UserCount = r.UserRoles.Count,
                PermissionCount = r.RolePermissions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return Result.Failure<RoleResponse>(RoleErrors.NotFound(query.RoleId));
        }

        return Result.Success(role);
    }
}
