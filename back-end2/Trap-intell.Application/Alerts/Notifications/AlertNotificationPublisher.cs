using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Alerts.Notifications;

internal static class AlertNotificationPublisher
{
    public static async Task PublishAsync(
        Alert alert,
        AlertNotificationType notificationType,
        string message,
        IUserRepository userRepository,
        INotificationDispatcher dispatcher,
        CancellationToken cancellationToken,
        params Guid[] additionalRecipientUserIds)
    {
        var recipients = await ResolveRecipientsAsync(alert, userRepository, cancellationToken, additionalRecipientUserIds);

        if (recipients.Count == 0)
        {
            return;
        }

        var title = BuildTitle(notificationType, alert);
        var priority = MapPriority(alert.Severity);
        var linkUri = $"/alerts/{alert.Id}";
        var relatedEntityId = alert.Id.ToString();

        foreach (var recipientId in recipients)
        {
            var notificationResult = Notification.Create(
                userId: recipientId,
                type: notificationType.ToString(),
                title: title,
                message: message,
                category: NotificationCategory.Alert,
                priority: priority,
                linkUri: linkUri,
                relatedEntityId: relatedEntityId);

            if (notificationResult.IsFailure)
            {
                continue;
            }

            await dispatcher.DispatchAsync(notificationResult.Value, cancellationToken);
        }
    }

    private static async Task<List<Guid>> ResolveRecipientsAsync(
        Alert alert,
        IUserRepository userRepository,
        CancellationToken cancellationToken,
        params Guid[] additionalRecipientUserIds)
    {
        var orgAdmins = await userRepository.GetByRoleAsync(
            alert.OrganizationId,
            SystemRoles.OrganizationAdminId,
            cancellationToken);

        var superAdminsInOrganization = await userRepository.GetByRoleAsync(
            alert.OrganizationId,
            SystemRoles.SuperAdminId,
            cancellationToken);

        var recipientIds = orgAdmins
            .Select(user => user.Id)
            .Concat(superAdminsInOrganization.Select(user => user.Id))
            .Concat(additionalRecipientUserIds)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        if (alert.AssignedToUserId.HasValue)
        {
            recipientIds.Add(alert.AssignedToUserId.Value);
        }

        return recipientIds.ToList();
    }

    private static string BuildTitle(AlertNotificationType notificationType, Alert alert)
    {
        return notificationType switch
        {
            AlertNotificationType.AlertCreated => $"New Alert: {alert.Title}",
            AlertNotificationType.AlertAssigned => $"Alert Assigned: {alert.Title}",
            AlertNotificationType.AlertAcknowledged => $"Alert Acknowledged: {alert.Title}",
            AlertNotificationType.AlertEscalated => $"Alert Escalated: {alert.Title}",
            AlertNotificationType.AlertResolved => $"Alert Resolved: {alert.Title}",
            AlertNotificationType.AlertMarkedFalsePositive => $"Alert Marked False Positive: {alert.Title}",
            AlertNotificationType.AlertSnoozed => $"Alert Snoozed: {alert.Title}",
            AlertNotificationType.AlertUnsnoozed => $"Alert Reopened: {alert.Title}",
            _ => $"Alert Update: {alert.Title}"
        };
    }

    private static NotificationPriority MapPriority(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => NotificationPriority.Critical,
            AlertSeverity.High => NotificationPriority.High,
            AlertSeverity.Medium => NotificationPriority.Normal,
            _ => NotificationPriority.Low
        };
    }
}
