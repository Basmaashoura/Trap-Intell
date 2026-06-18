namespace Trap_Intel.Domain.Notifications.Enums;

/// <summary>
/// Canonical notification types emitted from alert lifecycle events.
/// Stored in Notification.Type to keep strongly-typed semantics.
/// </summary>
public enum AlertNotificationType
{
    AlertCreated = 0,
    AlertAssigned = 1,
    AlertAcknowledged = 2,
    AlertEscalated = 3,
    AlertResolved = 4,
    AlertMarkedFalsePositive = 5,
    AlertSnoozed = 6,
    AlertUnsnoozed = 7
}
