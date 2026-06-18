using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.ActivateRole;

/// <summary>
/// Handler for ActivateRoleCommand.
/// Activates a deactivated role.
/// </summary>
internal sealed class ActivateRoleCommandHandler : ICommandHandler<ActivateRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public ActivateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ActivateRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        role.Activate();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
