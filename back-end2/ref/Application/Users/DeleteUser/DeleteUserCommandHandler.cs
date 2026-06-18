using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand.
/// Permanently deletes a user from the system.
/// </summary>
internal sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public DeleteUserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        // Prevent self-deletion
        if (command.UserId == _currentUser.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        User? user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Check if trying to delete a Super Admin
        bool isSuperAdmin = user.UserRoles.Any(ur => ur.RoleId == GetSuperAdminRoleId());
        if (isSuperAdmin)
        {
            return Result.Failure(UserErrors.CannotDeactivateSuperAdmin);
        }

        // Delete from Identity first
        Result identityResult = await _identityService.DeleteUserAsync(
            command.UserId,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            return identityResult;
        }

        // Remove from domain
        _context.Users.Remove(user);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static Guid GetSuperAdminRoleId()
    {
        // This should match the seeded Super Admin role ID
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
