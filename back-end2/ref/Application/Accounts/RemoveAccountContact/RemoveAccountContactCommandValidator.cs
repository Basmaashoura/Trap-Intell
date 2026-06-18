using FluentValidation;

namespace Application.Accounts.RemoveAccountContact;

/// <summary>
/// Validator for RemoveAccountContactCommand.
/// </summary>
internal sealed class RemoveAccountContactCommandValidator : AbstractValidator<RemoveAccountContactCommand>
{
    public RemoveAccountContactCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.ContactId)
            .NotEmpty()
            .WithMessage("Contact ID is required");
    }
}
