using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.DeleteRole;

/// <summary>
/// Handler for DeleteRoleCommand.
/// Permanently deletes a role from the system.
/// </summary>
internal sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        // Cannot delete system roles
        if (role.IsSystemRole)
        {
            return Result.Failure(RoleErrors.CannotModifySystemRole);
        }

        // Cannot delete roles with assigned users
        if (role.UserRoles.Count > 0)
        {
            return Result.Failure(RoleErrors.CannotDeleteRoleWithUsers);
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
