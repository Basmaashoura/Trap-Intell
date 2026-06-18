namespace Trap_Intel.Infrastructure.Notifications.RealTime;

public interface INotificationHubClient
{
    Task ReceiveNotification(object notificationDto);
    Task RefreshUnreadCount(int currentCount);
}
