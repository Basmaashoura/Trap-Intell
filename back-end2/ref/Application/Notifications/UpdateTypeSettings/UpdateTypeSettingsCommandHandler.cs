using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.UpdateTypeSettings;

internal sealed class UpdateTypeSettingsCommandHandler(
    INotificationTypeRepository typeRepository,
    IUserNotificationPreferencesRepository preferencesRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateTypeSettingsCommand>
{
    public async Task<Result> Handle(
        UpdateTypeSettingsCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        // Verify notification type exists
        NotificationType? type = await typeRepository.GetByIdAsync(request.TypeId, cancellationToken);

        if (type is null)
        {
            return Result.Failure(NotificationErrors.TypeNotFound(request.TypeId));
        }

        // System types cannot be disabled
        if (type.IsSystemType && !request.IsEnabled)
        {
            return Result.Failure(NotificationErrors.CannotDisableSystemType);
        }

        // Get or create setting
        UserNotificationTypeSetting? setting = await preferencesRepository.GetTypeSettingAsync(
            userId,
            request.TypeId,
            cancellationToken);

        // Parse channels
        NotificationChannel? channels = null;
        if (request.Channels is not null && request.Channels.Count > 0)
        {
            channels = NotificationChannel.None;
            foreach (string channelName in request.Channels)
            {
                if (Enum.TryParse<NotificationChannel>(channelName, true, out NotificationChannel channel))
                {
                    channels |= channel;
                }
            }
        }

        if (setting is null)
        {
            setting = UserNotificationTypeSetting.Create(
                userId,
                request.TypeId,
                request.IsEnabled,
                channels);
            preferencesRepository.AddTypeSetting(setting);
        }
        else
        {
            setting.Update(request.IsEnabled, channels);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
