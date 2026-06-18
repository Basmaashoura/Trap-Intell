using FluentValidation;

namespace Application.Notifications.RegisterPushToken;

public sealed class RegisterPushTokenCommandValidator : AbstractValidator<RegisterPushTokenCommand>
{
    private static readonly string[] ValidPlatforms = ["iOS", "Android", "Web", "Windows", "macOS"];

    public RegisterPushTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Push token is required")
            .MaximumLength(500).WithMessage("Push token must not exceed 500 characters");

        RuleFor(x => x.Platform)
            .NotEmpty().WithMessage("Platform is required")
            .Must(p => ValidPlatforms.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Platform must be one of: {string.Join(", ", ValidPlatforms)}");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DeviceName));
    }
}
