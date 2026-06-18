using FluentValidation;

namespace Application.Users.ResendConfirmation;

/// <summary>
/// Validator for ResendConfirmationCommand.
/// </summary>
internal sealed class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email address format");
    }
}
