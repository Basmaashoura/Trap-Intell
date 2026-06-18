using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.Enable2FA;

/// <summary>
/// Handler for Enable2FACommand.
/// Enables two-factor authentication and returns setup information.
/// </summary>
internal sealed class Enable2FACommandHandler : ICommandHandler<Enable2FACommand, Enable2FAResponse>
{
    private readonly IIdentityService _identityService;

    public Enable2FACommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<Enable2FAResponse>> Handle(
        Enable2FACommand command,
        CancellationToken cancellationToken)
    {
        // Enable 2FA and get setup information
        Result<TwoFactorSetupInfo> result =
            await _identityService.EnableTwoFactorAsync(command.UserId, cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Enable2FAResponse>(result.Error);
        }

        // Domain event (TwoFactorEnabledDomainEvent) is raised inside IdentityService

        return new Enable2FAResponse(
            result.Value.SharedSecret,
            result.Value.QrCodeUri);
    }
}
