using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error NotFound = Error.Custom(
        "Notification.NotFound",
        "The specified notification could not be found.");

    public static readonly Error Unauthorized = Error.Custom(
        "Notification.Unauthorized",
        "You do not have permission to access a notification belonging to another user.");

    public static readonly Error InvalidToken = Error.Custom(
        "Notification.InvalidToken",
        "The provided push notification token is invalid.");

    public static readonly Error TokenAlreadyRegistered = Error.Custom(
        "Notification.TokenAlreadyRegistered",
        "This push token is already registered to your account.");

    public static readonly Error TokenNotFound = Error.Custom(
        "Notification.TokenNotFound",
        "The specified push token could not be found.");

    public static readonly Error TitleRequired = Error.Custom(
        "Notification.TitleRequired",
        "The notification title cannot be empty.");

    public static readonly Error TargetUserRequired = Error.Custom(
        "Notification.TargetUserRequired",
        "A valid target user ID must be provided.");
}
