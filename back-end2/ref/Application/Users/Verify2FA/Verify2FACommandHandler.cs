using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.Verify2FA;

/// <summary>
/// Handler for Verify2FACommand.
/// Verifies the 2FA code to complete the setup process.
/// </summary>
internal sealed class Verify2FACommandHandler : ICommandHandler<Verify2FACommand>
{
    private readonly IIdentityService _identityService;

    public Verify2FACommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(Verify2FACommand command, CancellationToken cancellationToken)
    {
        // Verify and confirm 2FA setup
        Result result = await _identityService.ConfirmTwoFactorAsync(
            command.UserId,
            command.Code,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        return Result.Success();
    }
}
