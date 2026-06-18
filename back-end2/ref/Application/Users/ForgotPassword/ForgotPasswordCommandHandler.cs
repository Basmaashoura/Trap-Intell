using Application.Abstractions.Email;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.ForgotPassword;

/// <summary>
/// Handler for ForgotPasswordCommand.
/// Generates a password reset token and sends it via email.
/// </summary>
internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Generate password reset token
        Result<string> tokenResult = await _identityService.GeneratePasswordResetTokenAsync(
            command.Email,
            cancellationToken);

        if (tokenResult.IsFailure)
        {
            // SECURITY: Don't reveal if email exists or not
            // Return success even if user doesn't exist to prevent email enumeration
            // Add delay to make timing consistent
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return Result.Success();
        }

        // Send password reset email
        await _emailService.SendPasswordResetEmailAsync(
            command.Email,
            tokenResult.Value,
            cancellationToken);

        // SECURITY: Always return success (timing-safe)
        return Result.Success();
    }
}
