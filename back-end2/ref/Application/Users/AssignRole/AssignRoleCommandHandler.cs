using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.AssignRole;

/// <summary>
/// Handler for AssignRoleCommand.
/// Assigns a role to a user.
/// </summary>
internal sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AssignRoleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        User? user = await _context.Users
            .Include(u => u.UserRoles)
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

        // Check if role exists
        bool roleExists = await _context.Roles
            .AnyAsync(r => r.Id == command.RoleId && r.IsActive, cancellationToken);

        if (!roleExists)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        // Check if role is already assigned
        bool alreadyAssigned = user.UserRoles.Any(ur => ur.RoleId == command.RoleId);
        if (alreadyAssigned)
        {
            return Result.Failure(UserErrors.RoleAlreadyAssigned);
        }

        // Create user role assignment using domain factory method
        var userRole = UserRole.Create(command.UserId, command.RoleId, _currentUser.UserId);
        _context.UserRoles.Add(userRole);

        // Raise domain event
        user.Raise(new UserRoleAssignedDomainEvent(command.UserId, command.RoleId));

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
