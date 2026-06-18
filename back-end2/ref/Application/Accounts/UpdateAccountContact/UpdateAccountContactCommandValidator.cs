using FluentValidation;

namespace Application.Accounts.UpdateAccountContact;

/// <summary>
/// Validator for UpdateAccountContactCommand.
/// </summary>
internal sealed class UpdateAccountContactCommandValidator : AbstractValidator<UpdateAccountContactCommand>
{
    public UpdateAccountContactCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.ContactId)
            .NotEmpty()
            .WithMessage("Contact ID is required");

        RuleFor(x => x.Role)
            .MaximumLength(100)
            .WithMessage("Role must not exceed 100 characters")
            .When(x => x.Role is not null);
    }
}
