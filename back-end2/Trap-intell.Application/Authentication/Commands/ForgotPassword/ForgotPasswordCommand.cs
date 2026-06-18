using FluentValidation;
using MediatR;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Authentication.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}

internal sealed class ForgotPasswordCommandHandler(
    IIdentityService identityService) 
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // 1. Generate password reset token via IdentityService
        Result<string> tokenResult = await identityService.GeneratePasswordResetTokenAsync(command.Email, cancellationToken);

        if (tokenResult.IsFailure)
        {
            // SECURITY: Prevent email enumeration attacks by not revealing if the user exists.
            // Simulate a delay to prevent timing analysis.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return Result.Success();
        }

        // 2. Here we should send the email via an IEmailService or IEventPublisher.
        // For example: await emailService.SendPasswordResetEmailAsync(command.Email, tokenResult.Value, cancellationToken);
        // (You can wire up this dependency once the EmailService abstraction exists in the new project)

        return Result.Success();
    }
}
