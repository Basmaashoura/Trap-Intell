using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Notifications;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Notifications.Channels;
using Trap_Intel.Application.Abstractions.Notifications;
using DispatchNotificationPriority = Trap_Intel.Domain.Notifications.Enums.NotificationPriority;

namespace Trap_Intel.Infrastructure.Notifications.Services;

internal sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationDispatcher(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger,
        IEnumerable<INotificationChannel> channels)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _channels = channels;
    }

    public async Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // 1. Fetch user to check notification settings
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Cannot dispatch notification {NotificationId}. User {UserId} not found.", notification.Id, notification.UserId);
            return;
        }

        // 2. Evaluate User Notification Settings
        var isCritical = notification.Priority == DispatchNotificationPriority.Critical;
        var settings = user.NotificationSettings;
        var shouldBypassPreferences = ShouldBypassPreferencesForAdminAlert(user, notification);
        var isDebugTestNotification = IsDebugTestNotification(notification);
        var mappedNotificationType = MapToUserNotificationType(notification);

        if (!shouldBypassPreferences && !isDebugTestNotification && !settings.NotificationsEnabled)
        {
            _logger.LogInformation(
                "Skipping notification {NotificationId}. User {UserId} has all notifications disabled. Type={NotificationType}, Category={Category}",
                notification.Id,
                notification.UserId,
                notification.Type,
                notification.Category);
            return;
        }

        if (!shouldBypassPreferences &&
            !isDebugTestNotification &&
            mappedNotificationType.HasValue &&
            !settings.ShouldSendNotification(mappedNotificationType.Value, isCritical))
        {
            _logger.LogInformation(
                "Skipping notification {NotificationId}. User {UserId} disabled notification type {MappedType}. SourceType={NotificationType}",
                notification.Id,
                notification.UserId,
                mappedNotificationType.Value,
                notification.Type);
            return;
        }

        if (!shouldBypassPreferences &&
            !isDebugTestNotification &&
            mappedNotificationType.HasValue &&
            IsAlertNotificationType(mappedNotificationType.Value) &&
            IsBelowAlertSeverityThreshold(notification.Priority, settings.AlertSeverityThreshold))
        {
            _logger.LogInformation(
                "Skipping notification {NotificationId}. User {UserId} alert threshold {Threshold} is above notification priority {Priority}. Type={NotificationType}",
                notification.Id,
                notification.UserId,
                settings.AlertSeverityThreshold,
                notification.Priority,
                notification.Type);
            return;
        }

        if (!shouldBypassPreferences &&
            !isDebugTestNotification &&
            settings.IsInQuietHours() &&
            (!isCritical || !settings.AllowCriticalDuringQuietHours))
        {
            _logger.LogInformation("Skipping notification {NotificationId} for user {UserId} due to quiet hours.", notification.Id, notification.UserId);
            return;
        }

        // Persist first so inbox endpoints and unread counts reflect this notification.
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Dispatch to all enabled channels
        var enabledChannels = settings.GetEnabledChannels(isCritical);

        // Digest batching is not yet backed by a scheduler, so we suppress non-in-app immediate sends
        // for non-critical notifications when digest mode is enabled.
        if (!shouldBypassPreferences &&
            !isDebugTestNotification &&
            !isCritical &&
            settings.DigestFrequency != DigestFrequency.Immediate)
        {
            enabledChannels = enabledChannels
                .Where(channel => channel == Trap_Intel.Domain.Identity.Notifications.NotificationChannel.InApp)
                .ToList();

            _logger.LogInformation(
                "Digest mode {DigestFrequency} active for user {UserId}. Notification {NotificationId} will be in-app only for now.",
                settings.DigestFrequency,
                notification.UserId,
                notification.Id);
        }

        var mustForceInApp = shouldBypassPreferences || isDebugTestNotification;

        if (mustForceInApp && !enabledChannels.Contains(Trap_Intel.Domain.Identity.Notifications.NotificationChannel.InApp))
        {
            enabledChannels.Insert(0, Trap_Intel.Domain.Identity.Notifications.NotificationChannel.InApp);
        }

        var dispatchTasks = new List<Task>();

        foreach (var enabledChannel in enabledChannels)
        {
            var handler = _channels.FirstOrDefault(c => c.GetType().Name.StartsWith(enabledChannel.ToString(), StringComparison.OrdinalIgnoreCase));

            if (handler != null)
            {
                dispatchTasks.Add(handler.SendAsync(notification, cancellationToken));
            }
        }

        try
        {
            await Task.WhenAll(dispatchTasks);
            _logger.LogInformation("Dispatched notification {NotificationId} to user {UserId} across {ChannelCount} channels.", notification.Id, notification.UserId, dispatchTasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "One or more channels failed to dispatch notification {NotificationId} to user {UserId}.", notification.Id, notification.UserId);
        }
    }

    private static bool ShouldBypassPreferencesForAdminAlert(User user, Notification notification)
    {
        var isAdminRole = user.RoleId == SystemRoles.OrganizationAdminId || user.RoleId == SystemRoles.SuperAdminId;
        if (!isAdminRole)
        {
            return false;
        }

        var isAlertType = Enum.TryParse<AlertNotificationType>(notification.Type, ignoreCase: true, out _)
            || notification.Category == NotificationCategory.Alert;

        return isAlertType;
    }

    private static bool IsDebugTestNotification(Notification notification)
    {
        var type = notification.Type ?? string.Empty;
        return string.Equals(type, "DebugRealtimeTest", StringComparison.OrdinalIgnoreCase)
            || type.Contains("Debug", StringComparison.OrdinalIgnoreCase);
    }

    private static NotificationType? MapToUserNotificationType(Notification notification)
    {
        if (Enum.TryParse<NotificationType>(notification.Type, ignoreCase: true, out var directMatch))
        {
            return directMatch;
        }

        // Align alert-related custom types to closest user preference buckets.
        if (string.Equals(notification.Type, nameof(AlertNotificationType.AlertMarkedFalsePositive), StringComparison.OrdinalIgnoreCase))
        {
            return NotificationType.AlertResolution;
        }

        if (string.Equals(notification.Type, nameof(AlertNotificationType.AlertSnoozed), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(notification.Type, nameof(AlertNotificationType.AlertUnsnoozed), StringComparison.OrdinalIgnoreCase))
        {
            return NotificationType.AlertEscalation;
        }

        return notification.Category switch
        {
            NotificationCategory.Alert => NotificationType.AlertCreated,
            NotificationCategory.Security => NotificationType.SecurityAdvisory,
            NotificationCategory.Billing => NotificationType.SubscriptionExpiring,
            NotificationCategory.Team => NotificationType.ProductUpdate,
            NotificationCategory.System => NotificationType.Maintenance,
            _ => null
        };
    }

    private static bool IsAlertNotificationType(NotificationType type)
    {
        return type is NotificationType.AlertCreated
            or NotificationType.AlertEscalation
            or NotificationType.AlertAssignment
            or NotificationType.AlertResolution;
    }

    private static bool IsBelowAlertSeverityThreshold(DispatchNotificationPriority priority, AlertSeverityThreshold threshold)
    {
        var priorityRank = priority switch
        {
            DispatchNotificationPriority.Low => 1,
            DispatchNotificationPriority.Normal => 2,
            DispatchNotificationPriority.High => 3,
            DispatchNotificationPriority.Critical => 4,
            _ => 1
        };

        var thresholdRank = threshold switch
        {
            AlertSeverityThreshold.Info => 0,
            AlertSeverityThreshold.Low => 1,
            AlertSeverityThreshold.Medium => 2,
            AlertSeverityThreshold.High => 3,
            AlertSeverityThreshold.Critical => 4,
            _ => 2
        };

        return priorityRank < thresholdRank;
    }
}
