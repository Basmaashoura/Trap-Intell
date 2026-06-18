using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using SharedKernel;

namespace Application.Users.ChangePassword;

/// <summary>
/// Handler for ChangePasswordCommand.
/// Changes user password and revokes all existing tokens for security.
/// </summary>
internal sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // Change password through Identity service
        Result result = await _identityService.ChangePasswordAsync(
            command.UserId,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        // Domain event is raised inside IdentityService
        // All tokens are automatically revoked by IdentityService

        return Result.Success();
    }
}
