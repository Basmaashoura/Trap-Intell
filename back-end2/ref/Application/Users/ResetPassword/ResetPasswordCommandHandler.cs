using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.ResetPassword;

/// <summary>
/// Handler for ResetPasswordCommand.
/// Resets user password using the provided token.
/// </summary>
internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        // Reset password using token
        Result result = await _identityService.ResetPasswordAsync(
            command.Email,
            command.Token,
            command.NewPassword,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        // Domain event (PasswordResetCompletedDomainEvent) is raised inside IdentityService
        // All tokens are automatically revoked

        return Result.Success();
    }
}
