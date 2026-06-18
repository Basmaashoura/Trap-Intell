using FluentValidation;

namespace Application.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    private static readonly string[] ValidDigestFrequencies = ["Immediate", "Hourly", "Daily", "Weekly"];

    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.EmailDigestFrequency)
            .Must(f => string.IsNullOrEmpty(f) || ValidDigestFrequencies.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Digest frequency must be one of: {string.Join(", ", ValidDigestFrequencies)}");

        RuleFor(x => x.QuietHoursTimezone)
            .MaximumLength(100).WithMessage("Timezone must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.QuietHoursTimezone));

        When(x => x.QuietHoursEnabled, () =>
        {
            RuleFor(x => x.QuietHoursStart)
                .NotNull().WithMessage("Quiet hours start time is required when quiet hours are enabled");

            RuleFor(x => x.QuietHoursEnd)
                .NotNull().WithMessage("Quiet hours end time is required when quiet hours are enabled");
        });
    }
}
