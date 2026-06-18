using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Domain event raised when a notification is created.
/// </summary>
public sealed record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    Guid TypeId) : IDomainEvent;
