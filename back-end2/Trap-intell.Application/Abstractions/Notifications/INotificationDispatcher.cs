using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Abstractions.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default);
}
