using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.RemoveRole;

/// <summary>
/// Handler for RemoveRoleCommand.
/// Removes a role from a user.
/// </summary>
internal sealed class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RemoveRoleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveRoleCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Prevent modifying own roles
        if (command.UserId == _currentUser.UserId)
        {
            return Result.Failure(UserErrors.CannotModifyOwnRole);
        }

        // Find the user role assignment
        UserRole? userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == command.UserId && ur.RoleId == command.RoleId, cancellationToken);

        if (userRole is null)
        {
            return Result.Success(); // Role not assigned, nothing to remove
        }

        _context.UserRoles.Remove(userRole);

        // Raise domain event
        user.Raise(new UserRoleRemovedDomainEvent(command.UserId, command.RoleId));

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
