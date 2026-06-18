using FluentValidation;

namespace Application.Notifications.UpdateTypeSettings;

public sealed class UpdateTypeSettingsCommandValidator : AbstractValidator<UpdateTypeSettingsCommand>
{
    private static readonly string[] ValidChannels = ["InApp", "Email", "Push", "Sms"];

    public UpdateTypeSettingsCommandValidator()
    {
        RuleFor(x => x.TypeId)
            .NotEmpty().WithMessage("Type ID is required");

        RuleForEach(x => x.Channels)
            .Must(c => ValidChannels.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Each channel must be one of: {string.Join(", ", ValidChannels)}")
            .When(x => x.Channels is not null && x.Channels.Count > 0);
    }
}
