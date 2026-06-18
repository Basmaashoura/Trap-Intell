using FluentValidation;

namespace Application.Users.UpdateUserProfile;

/// <summary>
/// Validator for UpdateUserProfileCommand.
/// </summary>
internal sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Department)
            .IsInEnum()
            .WithMessage("Invalid department");

        RuleFor(x => x.DisplayJobTitle)
            .MaximumLength(100)
            .WithMessage("Display job title must not exceed 100 characters")
            .When(x => x.DisplayJobTitle is not null);

        RuleFor(x => x.InternalJobTitle)
            .MaximumLength(100)
            .WithMessage("Internal job title must not exceed 100 characters")
            .When(x => x.InternalJobTitle is not null);

        RuleFor(x => x.HourlyCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Hourly cost must be greater than or equal to 0")
            .When(x => x.HourlyCost.HasValue);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("Phone number must not exceed 50 characters")
            .Matches(@"^[\d\s\+\-\(\)]*$")
            .WithMessage("Phone number contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .WithMessage("Avatar URL must not exceed 500 characters")
            .Must(BeAValidUrl)
            .WithMessage("Avatar URL must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

        RuleFor(x => x.Bio)
            .MaximumLength(1000)
            .WithMessage("Bio must not exceed 1000 characters")
            .When(x => x.Bio is not null);

        RuleFor(x => x.Skills)
            .Must(skills => skills == null || skills.Count <= 20)
            .WithMessage("Cannot have more than 20 skills");

        RuleForEach(x => x.Skills)
            .MaximumLength(50)
            .WithMessage("Each skill must not exceed 50 characters")
            .When(x => x.Skills is not null);
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
