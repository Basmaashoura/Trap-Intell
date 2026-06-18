using FluentValidation;

namespace Application.Users.ForgotPassword;

/// <summary>
/// Validator for ForgotPasswordCommand.
/// </summary>
internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email address format");
    }
}
