using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Infrastructure.Notifications.Channels;

public interface INotificationChannel
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
