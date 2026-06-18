using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Domain errors for notification operations.
/// </summary>
public static class NotificationErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Notification.NotFound",
        $"The notification with Id = '{id}' was not found");

    public static Error TypeNotFound(Guid id) => Error.NotFound(
        "NotificationType.NotFound",
        $"The notification type with Id = '{id}' was not found");

    public static Error TypeNotFoundByCode(string code) => Error.NotFound(
        "NotificationType.NotFoundByCode",
        $"The notification type with Code = '{code}' was not found");

    public static Error PreferencesNotFound(Guid userId) => Error.NotFound(
        "UserNotificationPreferences.NotFound",
        $"Notification preferences for user with Id = '{userId}' were not found");

    public static Error PushTokenNotFound(Guid id) => Error.NotFound(
        "UserPushToken.NotFound",
        $"The push token with Id = '{id}' was not found");

    public static Error AlreadyRead => Error.Conflict(
        "Notification.AlreadyRead",
        "This notification has already been marked as read");

    public static Error AlreadyDismissed => Error.Conflict(
        "Notification.AlreadyDismissed",
        "This notification has already been dismissed");

    public static Error CannotDisableSystemType => Error.Validation(
        "NotificationType.CannotDisableSystemType",
        "System notification types cannot be disabled");

    public static Error InvalidChannel => Error.Validation(
        "Notification.InvalidChannel",
        "The specified notification channel is not valid");

    public static Error UserNotAuthorized => Error.Failure(
        "Notification.UserNotAuthorized",
        "You are not authorized to access this notification");

    public static Error DeliveryFailed(string reason) => Error.Failure(
        "Notification.DeliveryFailed",
        $"Failed to deliver notification: {reason}");

    public static Error TemplateProcessingFailed(string reason) => Error.Failure(
        "Notification.TemplateProcessingFailed",
        $"Failed to process notification template: {reason}");

    public static Error PushTokenAlreadyExists => Error.Conflict(
        "UserPushToken.AlreadyExists",
        "This push token is already registered");

    public static Error InvalidTimezone(string timezone) => Error.Validation(
        "UserNotificationPreferences.InvalidTimezone",
        $"The timezone '{timezone}' is not valid");

    public static Error QuietHoursInvalid => Error.Validation(
        "UserNotificationPreferences.QuietHoursInvalid",
        "Quiet hours start and end times must both be specified");
}
