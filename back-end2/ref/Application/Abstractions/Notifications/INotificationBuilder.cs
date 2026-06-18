using Domain.Notifications;

#pragma warning disable CA1054 // URI parameters should not be strings

namespace Application.Abstractions.Notifications;

/// <summary>
/// Builder interface for creating and sending notifications.
/// </summary>
public interface INotificationBuilder
{
    /// <summary>
    /// Sets the notification type by code.
    /// </summary>
    INotificationBuilder WithType(string typeCode);

    /// <summary>
    /// Adds a single recipient.
    /// </summary>
    INotificationBuilder ToUser(Guid userId);

    /// <summary>
    /// Adds multiple recipients.
    /// </summary>
    INotificationBuilder ToUsers(IEnumerable<Guid> userIds);

    /// <summary>
    /// Adds all users with a specific role as recipients.
    /// </summary>
    INotificationBuilder ToRole(string roleName);

    /// <summary>
    /// Sets template data for placeholders.
    /// </summary>
    INotificationBuilder WithData(object data);

    /// <summary>
    /// Sets a specific data value.
    /// </summary>
    INotificationBuilder WithData(string key, object value);

    /// <summary>
    /// Sets the priority (overrides type default).
    /// </summary>
    INotificationBuilder WithPriority(NotificationPriority priority);

    /// <summary>
    /// Sets the channels (overrides type default).
    /// </summary>
    INotificationBuilder WithChannels(NotificationChannel channels);

    /// <summary>
    /// Sets the related entity.
    /// </summary>
    INotificationBuilder WithEntity(string entityType, Guid entityId);

    /// <summary>
    /// Sets the action button.
    /// </summary>
    INotificationBuilder WithAction(string url, string text);

    /// <summary>
    /// Sets a group key for collapsing similar notifications.
    /// </summary>
    INotificationBuilder GroupBy(string groupKey);

    /// <summary>
    /// Schedules the notification for later.
    /// </summary>
    INotificationBuilder ScheduleFor(DateTime sendAt);

    /// <summary>
    /// Sets when the notification expires.
    /// </summary>
    INotificationBuilder ExpiresAt(DateTime expiresAt);

    /// <summary>
    /// Sets the user who triggered the notification.
    /// </summary>
    INotificationBuilder CreatedBy(Guid userId);

    /// <summary>
    /// Builds the notification context from a pre-built context.
    /// </summary>
    INotificationBuilder FromContext(NotificationContext context);

    /// <summary>
    /// Sends the notification.
    /// </summary>
    Task<NotificationResult> SendAsync(CancellationToken cancellationToken = default);
}
