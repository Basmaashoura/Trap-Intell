using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Roles;
using NotificationPreferenceType = Trap_Intel.Domain.Identity.Notifications.NotificationType;

namespace Trap_Intel.Application.Identity.Events;

internal sealed class UserAdministrationDomainEventNotificationHandler :
    INotificationHandler<UserActivatedEvent>,
    INotificationHandler<UserDeactivatedEvent>,
    INotificationHandler<UserSuspendedEvent>,
    INotificationHandler<UserUnsuspendedEvent>,
    INotificationHandler<UserRoleChangedEvent>,
    INotificationHandler<UserUnlockedEvent>,
    INotificationHandler<UserLockedOutEvent>
{
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<UserAdministrationDomainEventNotificationHandler> _logger;

    public UserAdministrationDomainEventNotificationHandler(
        INotificationDispatcher notificationDispatcher,
        ILogger<UserAdministrationDomainEventNotificationHandler> logger)
    {
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task Handle(UserActivatedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account reactivated",
            message: "Your account has been reactivated by an administrator.",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account deactivated",
            message: $"Your account was deactivated by an administrator. Reason: {notification.Reason}",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserSuspendedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account suspended",
            message: $"Your account has been suspended. Reason: {notification.Reason}",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserUnsuspendedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account unsuspended",
            message: "Your account suspension has been removed by an administrator.",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        var oldRoleName = ResolveRoleDisplayName(notification.OldRole);
        var newRoleName = ResolveRoleDisplayName(notification.NewRole);

        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Role updated",
            message: $"Your organization role changed from {oldRoleName} to {newRoleName}.",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserUnlockedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account unlocked",
            message: "Your account lockout has been cleared.",
            category: NotificationCategory.Security,
            priority: NotificationPriority.High,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserLockedOutEvent notification, CancellationToken cancellationToken)
    {
        await DispatchAsync(
            userId: notification.UserId,
            type: NotificationPreferenceType.SecurityAdvisory.ToString(),
            title: "Account locked due to failed sign-in attempts",
            message: $"Your account was temporarily locked after {notification.TotalFailedAttempts} failed sign-in attempts.",
            category: NotificationCategory.Security,
            priority: NotificationPriority.Critical,
            linkUri: "/profile/security",
            relatedEntityId: notification.UserId.ToString(),
            cancellationToken: cancellationToken);
    }

    private async Task DispatchAsync(
        Guid userId,
        string type,
        string title,
        string message,
        NotificationCategory category,
        NotificationPriority priority,
        string? linkUri,
        string? relatedEntityId,
        CancellationToken cancellationToken)
    {
        var notificationResult = Notification.Create(
            userId: userId,
            type: type,
            title: title,
            message: message,
            category: category,
            priority: priority,
            linkUri: linkUri,
            relatedEntityId: relatedEntityId);

        if (notificationResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create identity administration notification for user {UserId}. Type={Type}",
                userId,
                type);
            return;
        }

        try
        {
            await _notificationDispatcher.DispatchAsync(notificationResult.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to dispatch identity administration notification for user {UserId}. Type={Type}",
                userId,
                type);
        }
    }

    private static string ResolveRoleDisplayName(Guid roleId)
    {
        var roleName = SystemRoles.GetName(roleId);
        return roleName == "CustomRole" ? $"role ({roleId})" : roleName;
    }
}
