using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.SuspendUser;

/// <summary>
/// Handler for SuspendUserCommand.
/// Suspends a user temporarily.
/// </summary>
internal sealed class SuspendUserCommandHandler : ICommandHandler<SuspendUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public SuspendUserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SuspendUserCommand command, CancellationToken cancellationToken)
    {
        // Prevent self-suspension
        if (command.UserId == _currentUser.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Check if user is already suspended
        if (user.Status == UserStatus.Suspended)
        {
            return Result.Failure(UserErrors.UserSuspended);
        }

        // Suspend in domain (raises UserSuspendedDomainEvent)
        user.Suspend();

        // Revoke all tokens in Identity
        Result identityResult = await _identityService.RevokeAllTokensAsync(
            command.UserId,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            return identityResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
