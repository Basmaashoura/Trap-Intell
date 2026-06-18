using FluentValidation;

namespace Application.Accounts.InviteContact;

/// <summary>
/// Validator for InviteContactCommand.
/// </summary>
public sealed class InviteContactCommandValidator : AbstractValidator<InviteContactCommand>
{
    public InviteContactCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .MaximumLength(100)
            .WithMessage("Role cannot exceed 100 characters.")
            .When(x => x.Role is not null);

        RuleFor(x => x.ExpirationDays)
            .InclusiveBetween(1, 30)
            .WithMessage("Expiration days must be between 1 and 30.");
    }
}
