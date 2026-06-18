using FluentValidation;

namespace Application.Users.ConfirmEmail;

/// <summary>
/// Validator for ConfirmEmailCommand.
/// </summary>
internal sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email address format");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Confirmation token is required");
    }
}
