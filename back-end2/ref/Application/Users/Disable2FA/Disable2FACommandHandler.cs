using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.Disable2FA;

/// <summary>
/// Handler for Disable2FACommand.
/// Disables two-factor authentication after password verification.
/// </summary>
internal sealed class Disable2FACommandHandler : ICommandHandler<Disable2FACommand>
{
    private readonly IIdentityService _identityService;

    public Disable2FACommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(Disable2FACommand command, CancellationToken cancellationToken)
    {
        // Disable 2FA using 2FA code for verification
        Result result = await _identityService.DisableTwoFactorAsync(
            command.UserId,
            command.Code,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        // Domain event (TwoFactorDisabledDomainEvent) is raised inside IdentityService

        return Result.Success();
    }
}
