using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.DeactivateUser;

/// <summary>
/// Handler for DeactivateUserCommand.
/// Deactivates a user as part of the Offboarding Protocol.
/// </summary>
internal sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public DeactivateUserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        // Prevent self-deactivation
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

        // Check if user is already inactive
        if (user.Status == UserStatus.Inactive)
        {
            return Result.Failure(UserErrors.UserInactive);
        }

        // Check if trying to deactivate a Super Admin
        bool isSuperAdmin = user.UserRoles.Any(ur => ur.RoleId == GetSuperAdminRoleId());
        if (isSuperAdmin)
        {
            return Result.Failure(UserErrors.CannotDeactivateSuperAdmin);
        }

        // Deactivate in domain (raises UserDeactivatedDomainEvent)
        user.Deactivate();

        // Deactivate in Identity (revokes all tokens)
        Result identityResult = await _identityService.DeactivateUserAsync(
            command.UserId,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            return identityResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static Guid GetSuperAdminRoleId()
    {
        // This should match the seeded Super Admin role ID
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
