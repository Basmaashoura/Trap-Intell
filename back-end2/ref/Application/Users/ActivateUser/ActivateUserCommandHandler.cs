using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ActivateUser;

/// <summary>
/// Handler for ActivateUserCommand.
/// Activates a previously deactivated user.
/// </summary>
internal sealed class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public ActivateUserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result> Handle(ActivateUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Check if user is already active
        if (user.Status == UserStatus.Active)
        {
            return Result.Success(); // Already active, no action needed
        }

        // Activate in domain (raises UserActivatedDomainEvent)
        user.Activate();

        // Activate in Identity
        Result identityResult = await _identityService.ActivateUserAsync(
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
