using FluentValidation;
using MediatR;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Authentication.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email, 
    string Token, 
    string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

internal sealed class ResetPasswordCommandHandler(
    IIdentityService identityService) 
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        // 1. Reset password via Identity Service utilizing the secure token
        var result = await identityService.ResetPasswordAsync(
            command.Email, 
            command.Token, 
            command.NewPassword, 
            cancellationToken);

        return result;
    }
}
