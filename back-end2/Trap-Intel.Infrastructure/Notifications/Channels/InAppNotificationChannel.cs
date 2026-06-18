using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Infrastructure.Notifications.RealTime;

namespace Trap_Intel.Infrastructure.Notifications.Channels;

internal sealed class InAppNotificationChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub, INotificationHubClient> _hubContext;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<InAppNotificationChannel> _logger;

    public InAppNotificationChannel(
        IHubContext<NotificationHub, INotificationHubClient> hubContext,
        IListRealtimeNotifier listRealtimeNotifier,
        INotificationRepository notificationRepository,
        ILogger<InAppNotificationChannel> logger)
    {
        _hubContext = hubContext;
        _listRealtimeNotifier = listRealtimeNotifier;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending In-App REAL-TIME Notification to User {UserId}: [{Title}]", 
            notification.UserId, notification.Title);

        try
        {
            var targetGroup = $"User_{notification.UserId}";

            // Map domain object to Dto format for UI consumption
            var payload = new 
            {
                id = notification.Id,
                type = notification.Type,
                category = notification.Category.ToString(),
                priority = notification.Priority.ToString(),
                title = notification.Title,
                message = notification.Message,
                linkUri = notification.LinkUri,
                createdAt = notification.CreatedAt,
                isRead = notification.IsRead
            };

            await _hubContext.Clients.Group(targetGroup).ReceiveNotification(payload);

            var unreadCount = await _notificationRepository.GetUnreadCountAsync(notification.UserId, cancellationToken);
            await _hubContext.Clients.Group(targetGroup).RefreshUnreadCount(unreadCount);

            var listPayload = new { notificationId = notification.Id };
            await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", notification.UserId, action: "created", payload: listPayload, cancellationToken: cancellationToken);
            await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", notification.UserId, filterKey: "unread=true", action: "created", payload: listPayload, cancellationToken: cancellationToken);

            _logger.LogInformation("SignalR dispatch completed for user {UserId}", notification.UserId);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to push real-time notification to user {UserId}", notification.UserId);
        }
    }
}
