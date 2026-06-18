using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Infrastructure.Authentication.Services;

namespace Trap_Intel.Infrastructure.Notifications.Channels;

internal sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<EmailNotificationChannel> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (!ShouldSendEmail(notification))
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "Skipping email notification because target user {UserId} was not found.",
                notification.UserId);
            return;
        }

        var emailAddress = user.Email.Value;
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return;
        }

        var displayName = ResolveDisplayName(user);
        var emailTitle = $"{notification.Category}: {notification.Title}";
        var details = BuildNotificationDetails(notification);

        if (notification.Category is NotificationCategory.Alert or NotificationCategory.Security)
        {
            await _emailService.SendSecurityAlertAsync(
                emailAddress,
                displayName,
                emailTitle,
                details,
                cancellationToken);
        }
        else
        {
            await _emailService.SendPlatformNotificationAsync(
                emailAddress,
                displayName,
                emailTitle,
                details,
                cancellationToken);
        }

        _logger.LogInformation(
            "EMAIL notification sent to User {UserId}: [{Title}]",
            notification.UserId,
            notification.Title);
    }

    private static bool ShouldSendEmail(Notification notification)
    {
        return notification.Priority is NotificationPriority.High or NotificationPriority.Critical
            || notification.Category is NotificationCategory.Alert or NotificationCategory.Security or NotificationCategory.Billing;
    }

    private static string ResolveDisplayName(User user)
    {
        var fullName = user.FullName?.Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName.Length <= 120 ? fullName : fullName[..120];
        }

        var email = user.Email.Value;
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "User";
        }

        var localPart = email[..atIndex].Trim();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            return "User";
        }

        return localPart.Length <= 80 ? localPart : localPart[..80];
    }

    private static string BuildNotificationDetails(Notification notification)
    {
        var message = notification.Message?.Trim() ?? string.Empty;
        var details =
            $"Notification type: {notification.Type}{Environment.NewLine}" +
            $"Category: {notification.Category}{Environment.NewLine}" +
            $"Priority: {notification.Priority}{Environment.NewLine}" +
            $"Created (UTC): {notification.CreatedAt:yyyy-MM-dd HH:mm:ss}";

        if (!string.IsNullOrWhiteSpace(message))
        {
            details += $"{Environment.NewLine}Message: {message}";
        }

        if (!string.IsNullOrWhiteSpace(notification.LinkUri))
        {
            details += $"{Environment.NewLine}Open in dashboard: {notification.LinkUri.Trim()}";
        }

        return details.Length <= 1800 ? details : details[..1800];
    }

}
