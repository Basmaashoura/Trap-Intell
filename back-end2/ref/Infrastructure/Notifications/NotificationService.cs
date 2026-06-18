using Application.Abstractions.Data;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// Main notification service implementation.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTypeRepository _typeRepository;
    private readonly IUserNotificationPreferencesRepository _preferencesRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationTypeRepository typeRepository,
        IUserNotificationPreferencesRepository preferencesRepository,
        INotificationDispatcher dispatcher,
        IRealtimeNotificationService realtimeService,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _notificationRepository = notificationRepository;
        _typeRepository = typeRepository;
        _preferencesRepository = preferencesRepository;
        _dispatcher = dispatcher;
        _realtimeService = realtimeService;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public INotificationBuilder Create(string notificationTypeCode)
    {
        return new NotificationBuilder(
            _notificationRepository,
            _typeRepository,
            _dispatcher,
            _realtimeService,
            _unitOfWork,
            _context)
            .WithType(notificationTypeCode);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        Notification? notification = await _notificationRepository.GetByIdAsync(notificationId, userId, cancellationToken);

        if (notification is not null)
        {
            notification.MarkAsRead();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<Notification> unreadNotifications = await _notificationRepository.GetUnreadAsync(userId, cancellationToken);

        foreach (Notification notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return unreadNotifications.Count;
    }

    public async Task DismissAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        Notification? notification = await _notificationRepository.GetByIdAsync(notificationId, userId, cancellationToken);

        if (notification is not null)
        {
            notification.Dismiss();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        List<Notification> expiredNotifications = await _notificationRepository.GetExpiredAsync(cancellationToken);

        foreach (Notification notification in expiredNotifications)
        {
            notification.Dismiss();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expiredNotifications.Count;
    }

    public async Task<int> SendScheduledNotificationsAsync(CancellationToken cancellationToken = default)
    {
        List<Notification> scheduledNotifications = await _notificationRepository.GetScheduledReadyToSendAsync(cancellationToken);

        int sentCount = 0;

        DateTime utcNow = DateTime.UtcNow;

        foreach (Notification notification in scheduledNotifications)
        {
            UserNotificationPreferences? preferences = await _preferencesRepository.GetByUserIdAsync(
                notification.UserId,
                cancellationToken);

            UserNotificationTypeSetting? typeSetting = await _context.UserNotificationTypeSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == notification.UserId && s.TypeId == notification.TypeId, cancellationToken);

            NotificationChannel channels = notification.Type?.DefaultChannels ?? NotificationChannel.InApp;
            bool saveRequired = false;
            bool shouldDispatch = true;

            if (typeSetting is not null)
            {
                if (!typeSetting.IsEnabled && notification.Type is not null && !notification.Type.IsSystemType)
                {
                    notification.Dismiss();
                    shouldDispatch = false;
                    saveRequired = true;
                }
                else if (typeSetting.Channels.HasValue)
                {
                    channels = typeSetting.Channels.Value;
                }
            }

            if (shouldDispatch && preferences is not null)
            {
                channels &= preferences.GetEnabledChannels();
            }

            if (shouldDispatch && channels == NotificationChannel.None)
            {
                notification.Dismiss();
                shouldDispatch = false;
                saveRequired = true;
            }

            if (shouldDispatch && notification.Priority != NotificationPriority.Urgent && preferences is not null)
            {
                DateTime? quietResume = preferences.GetQuietHoursResumeTime(utcNow);

                if (quietResume.HasValue)
                {
                    notification.ScheduleFor(quietResume.Value);
                    shouldDispatch = false;
                    saveRequired = true;
                }
            }

            if (!shouldDispatch)
            {
                if (saveRequired)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                continue;
            }

            await _dispatcher.DispatchAsync(notification, channels, cancellationToken);

            if (channels.HasFlag(NotificationChannel.InApp))
            {
                var realtimeMessage = RealtimeNotificationMessage.FromNotification(notification, notification.Type);
                await _realtimeService.SendToUserAsync(notification.UserId, realtimeMessage, cancellationToken);

                int unreadCount = await _notificationRepository.GetUnreadCountAsync(notification.UserId, cancellationToken);
                await _realtimeService.NotifyUnreadCountAsync(notification.UserId, unreadCount, cancellationToken);
            }

            sentCount++;
        }

        return sentCount;
    }
}
