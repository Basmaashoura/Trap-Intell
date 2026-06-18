using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.UpdatePreferences;

internal sealed class UpdatePreferencesCommandHandler(
    IUserNotificationPreferencesRepository preferencesRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdatePreferencesCommand>
{
    public async Task<Result> Handle(
        UpdatePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        UserNotificationPreferences preferences = await preferencesRepository.GetOrCreateDefaultAsync(
            userId,
            cancellationToken);

        // Validate timezone if provided
        if (request.QuietHoursEnabled && !string.IsNullOrEmpty(request.QuietHoursTimezone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.QuietHoursTimezone);
            }
            catch (TimeZoneNotFoundException)
            {
                return Result.Failure(NotificationErrors.InvalidTimezone(request.QuietHoursTimezone));
            }
        }

        // Validate quiet hours
        if (request.QuietHoursEnabled &&
            (!request.QuietHoursStart.HasValue || !request.QuietHoursEnd.HasValue))
        {
            return Result.Failure(NotificationErrors.QuietHoursInvalid);
        }

        // Parse digest frequency
        if (!Enum.TryParse<DigestFrequency>(request.EmailDigestFrequency, true, out DigestFrequency digestFrequency))
        {
            digestFrequency = DigestFrequency.Daily;
        }

        // Update preferences
        preferences.UpdateChannelSettings(
            request.InAppEnabled,
            request.EmailEnabled,
            request.PushEnabled,
            request.SmsEnabled);

        preferences.UpdateDigestSettings(
            request.EmailDigestEnabled,
            digestFrequency,
            request.EmailDigestTime);

        preferences.UpdateQuietHours(
            request.QuietHoursEnabled,
            request.QuietHoursStart,
            request.QuietHoursEnd,
            request.QuietHoursTimezone);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
