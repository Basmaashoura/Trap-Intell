using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Notifications.Common;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetPreferences;

internal sealed class GetPreferencesQueryHandler(
    IUserNotificationPreferencesRepository preferencesRepository,
    INotificationTypeRepository typeRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPreferencesQuery, NotificationPreferencesResponse>
{
    public async Task<Result<NotificationPreferencesResponse>> Handle(
        GetPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        UserNotificationPreferences preferences = await preferencesRepository.GetOrCreateDefaultAsync(
            userId,
            cancellationToken);

        // Get type settings
        List<UserNotificationTypeSetting> typeSettings = await preferencesRepository.GetTypeSettingsAsync(
            userId,
            cancellationToken);

        // Get all active notification types
        List<NotificationType> allTypes = await typeRepository.GetAllActiveAsync(cancellationToken);

        // Build type settings response
        var typeSettingsResponse = allTypes.Select(type =>
        {
            UserNotificationTypeSetting? setting = typeSettings.Find(s => s.TypeId == type.Id);
            NotificationChannel channels = setting?.Channels ?? type.DefaultChannels;

            return new NotificationTypeSettingResponse(
                type.Id,
                type.Code,
                type.Name,
                setting?.IsEnabled ?? true,
                GetChannelNames(channels));
        }).ToList();

        var response = new NotificationPreferencesResponse(
            preferences.InAppEnabled,
            preferences.EmailEnabled,
            preferences.PushEnabled,
            preferences.SmsEnabled,
            preferences.EmailDigestEnabled,
            preferences.EmailDigestFrequency.ToString(),
            preferences.EmailDigestTime,
            preferences.QuietHoursEnabled,
            preferences.QuietHoursStart,
            preferences.QuietHoursEnd,
            preferences.QuietHoursTimezone,
            typeSettingsResponse);

        return Result.Success(response);
    }

    private static List<string> GetChannelNames(NotificationChannel channels)
    {
        var names = new List<string>();

        if (channels.HasFlag(NotificationChannel.InApp))
        {
            names.Add("InApp");
        }
        if (channels.HasFlag(NotificationChannel.Email))
        {
            names.Add("Email");
        }
        if (channels.HasFlag(NotificationChannel.Push))
        {
            names.Add("Push");
        }
        if (channels.HasFlag(NotificationChannel.Sms))
        {
            names.Add("Sms");
        }

        return names;
    }
}
