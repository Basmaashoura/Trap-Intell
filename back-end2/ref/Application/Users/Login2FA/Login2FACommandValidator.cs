using FluentValidation;

namespace Application.Users.Login2FA;

internal sealed class Login2FACommandValidator : AbstractValidator<Login2FACommand>
{
    public Login2FACommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("2FA code is required.")
            .Length(6).WithMessage("2FA code must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("2FA code must contain only digits.");
    }
}
