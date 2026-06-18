using FluentValidation;

namespace Application.Users.Disable2FA;

/// <summary>
/// Validator for Disable2FACommand.
/// </summary>
internal sealed class Disable2FACommandValidator : AbstractValidator<Disable2FACommand>
{
    public Disable2FACommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("2FA code is required.")
            .Length(6).WithMessage("2FA code must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("2FA code must contain only digits.");
    }
}
