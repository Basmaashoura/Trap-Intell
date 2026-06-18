using FluentValidation;

namespace Application.Accounts.UpdateContactPermissions;

/// <summary>
/// Validator for UpdateContactPermissionsCommand.
/// </summary>
public sealed class UpdateContactPermissionsCommandValidator : AbstractValidator<UpdateContactPermissionsCommand>
{
    public UpdateContactPermissionsCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.ContactId)
            .NotEmpty()
            .WithMessage("Contact ID is required.");
    }
}
