using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Domain event raised when a notification is marked as read.
/// </summary>
public sealed record NotificationReadDomainEvent(
    Guid NotificationId,
    Guid UserId) : IDomainEvent;
