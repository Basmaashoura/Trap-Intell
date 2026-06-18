using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Notifications.Events;

/// <summary>
/// Domain events related to generic system notifications.
/// All events must implement IDomainEvent for dispatcher publishing.
/// </summary>

public record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    DateTime OccurredOn) : IDomainEvent
{
    public NotificationCreatedDomainEvent(Guid notificationId, Guid userId)
        : this(notificationId, userId, DateTime.UtcNow) { }
}

public record NotificationReadDomainEvent(
    Guid NotificationId,
    Guid UserId,
    DateTime OccurredOn) : IDomainEvent
{
    public NotificationReadDomainEvent(Guid notificationId, Guid userId)
        : this(notificationId, userId, DateTime.UtcNow) { }
}

public record NotificationDismissedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    DateTime OccurredOn) : IDomainEvent
{
    public NotificationDismissedDomainEvent(Guid notificationId, Guid userId)
        : this(notificationId, userId, DateTime.UtcNow) { }
}
