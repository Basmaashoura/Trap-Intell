using FluentValidation;

namespace Application.Accounts.UpdateAccount;

/// <summary>
/// Validator for UpdateAccountCommand.
/// </summary>
internal sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Account name is required")
            .MaximumLength(200)
            .WithMessage("Account name must not exceed 200 characters");

        RuleFor(x => x.Industry)
            .MaximumLength(100)
            .WithMessage("Industry must not exceed 100 characters")
            .When(x => x.Industry is not null);

        RuleFor(x => x.Website)
            .MaximumLength(500)
            .WithMessage("Website must not exceed 500 characters")
            .Must(BeAValidUrl)
            .WithMessage("Website must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .WithMessage("Phone must not exceed 50 characters")
            .Matches(@"^[\d\s\+\-\(\)]+$")
            .WithMessage("Phone contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address must not exceed 500 characters")
            .When(x => x.Address is not null);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50)
            .WithMessage("Tax number must not exceed 50 characters")
            .When(x => x.TaxNumber is not null);
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out Uri? result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
