using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.DeactivateRole;

/// <summary>
/// Handler for DeactivateRoleCommand.
/// Deactivates a role (soft delete).
/// </summary>
internal sealed class DeactivateRoleCommandHandler : ICommandHandler<DeactivateRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeactivateRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        // Domain method handles system role check and raises event
        Result result = role.Deactivate();

        if (result.IsFailure)
        {
            return result;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
