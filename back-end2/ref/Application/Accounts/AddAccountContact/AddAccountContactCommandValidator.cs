using FluentValidation;

namespace Application.Accounts.AddAccountContact;

/// <summary>
/// Validator for AddAccountContactCommand.
/// </summary>
internal sealed class AddAccountContactCommandValidator : AbstractValidator<AddAccountContactCommand>
{
    public AddAccountContactCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Role)
            .MaximumLength(100)
            .WithMessage("Role must not exceed 100 characters")
            .When(x => x.Role is not null);
    }
}
