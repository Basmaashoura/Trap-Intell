using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.UpdateRolePermissions;

/// <summary>
/// Handler for UpdateRolePermissionsCommand.
/// Updates permissions assigned to a role (replaces all).
/// </summary>
internal sealed class UpdateRolePermissionsCommandHandler : ICommandHandler<UpdateRolePermissionsCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateRolePermissionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        // System roles cannot be modified
        if (role.IsSystemRole)
        {
            return Result.Failure(RoleErrors.CannotModifySystemRole);
        }

        // Validate all permission IDs exist
        var requestedPermissionIds = command.PermissionIds.ToHashSet();
        List<Guid> existingPermissionIds = await _context.Permissions
            .Where(p => requestedPermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var missingPermissionIds = requestedPermissionIds.Except(existingPermissionIds).ToHashSet();
        if (missingPermissionIds.Count > 0)
        {
            return Result.Failure(PermissionErrors.NotFound(missingPermissionIds.First()));
        }

        // Remove existing permissions
        _context.RolePermissions.RemoveRange(role.RolePermissions);

        // Add new permissions using domain factory method
        foreach (Guid permissionId in command.PermissionIds.Distinct())
        {
            var rolePermission = RolePermission.Create(role.Id, permissionId);
            _context.RolePermissions.Add(rolePermission);
        }

        // Raise domain event
        role.Raise(new RolePermissionsUpdatedDomainEvent(role.Id));

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
