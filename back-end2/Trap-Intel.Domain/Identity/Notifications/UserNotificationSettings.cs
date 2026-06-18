namespace Trap_Intel.Domain.Identity.Notifications;

/// <summary>
/// User notification preferences - controls what notifications user receives and how.
/// Value object owned by User aggregate.
/// </summary>
public record UserNotificationSettings
{
    #region Global Settings

    /// <summary>
    /// Whether all notifications are enabled.
    /// </summary>
    public bool NotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Whether to send email notifications.
    /// </summary>
    public bool EmailNotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Whether to send SMS notifications (for critical alerts).
    /// </summary>
    public bool SmsNotificationsEnabled { get; init; } = false;

    /// <summary>
    /// Whether to show in-app notifications.
    /// </summary>
    public bool InAppNotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Whether to send push notifications (mobile/browser).
    /// </summary>
    public bool PushNotificationsEnabled { get; init; } = true;

    #endregion

    #region Alert Notifications

    /// <summary>
    /// Receive notifications for new alerts.
    /// </summary>
    public bool AlertCreatedNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for alert escalations.
    /// </summary>
    public bool AlertEscalationNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for alert assignments.
    /// </summary>
    public bool AlertAssignmentNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for alert resolutions.
    /// </summary>
    public bool AlertResolutionNotification { get; init; } = false;

    /// <summary>
    /// Minimum severity for alert notifications.
    /// </summary>
    public AlertSeverityThreshold AlertSeverityThreshold { get; init; } = AlertSeverityThreshold.Medium;

    #endregion

    #region Attack Notifications

    /// <summary>
    /// Receive notifications for high severity attacks.
    /// </summary>
    public bool HighSeverityAttackNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for malware detection.
    /// </summary>
    public bool MalwareDetectionNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for brute force attempts.
    /// </summary>
    public bool BruteForceNotification { get; init; } = true;

    #endregion

    #region Threat Actor Notifications

    /// <summary>
    /// Receive notifications for new threat actors.
    /// </summary>
    public bool NewThreatActorNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for threat level escalations.
    /// </summary>
    public bool ThreatLevelEscalationNotification { get; init; } = true;

    #endregion

    #region Honeypot Notifications

    /// <summary>
    /// Receive notifications when honeypots go offline.
    /// </summary>
    public bool HoneypotOfflineNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for honeypot health issues.
    /// </summary>
    public bool HoneypotHealthNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for storage warnings.
    /// </summary>
    public bool StorageWarningNotification { get; init; } = true;

    #endregion

    #region System Notifications

    /// <summary>
    /// Receive notifications for quota warnings.
    /// </summary>
    public bool QuotaWarningNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for subscription expiring.
    /// </summary>
    public bool SubscriptionExpiringNotification { get; init; } = true;

    /// <summary>
    /// Receive notifications for system maintenance.
    /// </summary>
    public bool MaintenanceNotification { get; init; } = true;

    /// <summary>
    /// Receive weekly summary reports.
    /// </summary>
    public bool WeeklySummaryEnabled { get; init; } = true;

    /// <summary>
    /// Receive monthly summary reports.
    /// </summary>
    public bool MonthlySummaryEnabled { get; init; } = true;

    #endregion

    #region Communication Preferences

    /// <summary>
    /// Receive product updates and announcements.
    /// </summary>
    public bool ProductUpdatesEnabled { get; init; } = true;

    /// <summary>
    /// Receive security advisories.
    /// </summary>
    public bool SecurityAdvisoriesEnabled { get; init; } = true;

    /// <summary>
    /// Receive tips and best practices.
    /// </summary>
    public bool TipsAndBestPracticesEnabled { get; init; } = false;

    #endregion

    #region Quiet Hours

    /// <summary>
    /// Whether quiet hours are enabled.
    /// </summary>
    public bool QuietHoursEnabled { get; init; } = false;

    /// <summary>
    /// Start of quiet hours (hour of day, 0-23).
    /// </summary>
    public int QuietHoursStart { get; init; } = 22;

    /// <summary>
    /// End of quiet hours (hour of day, 0-23).
    /// </summary>
    public int QuietHoursEnd { get; init; } = 7;

    /// <summary>
    /// Timezone for quiet hours.
    /// </summary>
    public string QuietHoursTimezone { get; init; } = "UTC";

    /// <summary>
    /// Allow critical alerts during quiet hours.
    /// </summary>
    public bool AllowCriticalDuringQuietHours { get; init; } = true;

    #endregion

    #region Digest Settings

    /// <summary>
    /// Digest frequency for non-critical notifications.
    /// </summary>
    public DigestFrequency DigestFrequency { get; init; } = DigestFrequency.Immediate;

    /// <summary>
    /// Preferred time for daily digest (hour of day).
    /// </summary>
    public int DailyDigestHour { get; init; } = 9;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create default notification settings.
    /// </summary>
    public static UserNotificationSettings Default() => new();

    /// <summary>
    /// Create minimal notification settings (critical only).
    /// </summary>
    public static UserNotificationSettings Minimal() => new()
    {
        AlertCreatedNotification = false,
        AlertResolutionNotification = false,
        AlertSeverityThreshold = AlertSeverityThreshold.Critical,
        BruteForceNotification = false,
        NewThreatActorNotification = false,
        HoneypotHealthNotification = false,
        StorageWarningNotification = false,
        WeeklySummaryEnabled = false,
        ProductUpdatesEnabled = false,
        TipsAndBestPracticesEnabled = false
    };

    /// <summary>
    /// Create security-focused notification settings.
    /// </summary>
    public static UserNotificationSettings SecurityFocused() => new()
    {
        AlertCreatedNotification = true,
        AlertEscalationNotification = true,
        AlertSeverityThreshold = AlertSeverityThreshold.Low,
        HighSeverityAttackNotification = true,
        MalwareDetectionNotification = true,
        BruteForceNotification = true,
        NewThreatActorNotification = true,
        ThreatLevelEscalationNotification = true,
        SmsNotificationsEnabled = true,
        QuietHoursEnabled = false,
        SecurityAdvisoriesEnabled = true
    };

    /// <summary>
    /// Create all-off settings (unsubscribe from all).
    /// </summary>
    public static UserNotificationSettings AllOff() => new()
    {
        NotificationsEnabled = false,
        EmailNotificationsEnabled = false,
        SmsNotificationsEnabled = false,
        PushNotificationsEnabled = false,
        AlertCreatedNotification = false,
        AlertEscalationNotification = false,
        AlertAssignmentNotification = false,
        AlertResolutionNotification = false,
        HighSeverityAttackNotification = false,
        MalwareDetectionNotification = false,
        BruteForceNotification = false,
        NewThreatActorNotification = false,
        ThreatLevelEscalationNotification = false,
        HoneypotOfflineNotification = false,
        HoneypotHealthNotification = false,
        StorageWarningNotification = false,
        QuotaWarningNotification = false,
        SubscriptionExpiringNotification = false,
        MaintenanceNotification = false,
        WeeklySummaryEnabled = false,
        MonthlySummaryEnabled = false,
        ProductUpdatesEnabled = false,
        SecurityAdvisoriesEnabled = false,
        TipsAndBestPracticesEnabled = false
    };

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if currently in quiet hours.
    /// </summary>
    public bool IsInQuietHours()
    {
        if (!QuietHoursEnabled) return false;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(QuietHoursTimezone);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var currentHour = now.Hour;

            if (QuietHoursStart <= QuietHoursEnd)
            {
                // Same day (e.g., 9:00 to 17:00)
                return currentHour >= QuietHoursStart && currentHour < QuietHoursEnd;
            }
            else
            {
                // Overnight (e.g., 22:00 to 7:00)
                return currentHour >= QuietHoursStart || currentHour < QuietHoursEnd;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if notification should be sent for given type and severity.
    /// </summary>
    public bool ShouldSendNotification(NotificationType type, bool isCritical = false)
    {
        if (!NotificationsEnabled) return false;

        // Allow critical during quiet hours if enabled
        if (IsInQuietHours() && (!isCritical || !AllowCriticalDuringQuietHours))
            return false;

        return type switch
        {
            NotificationType.AlertCreated => AlertCreatedNotification,
            NotificationType.AlertEscalation => AlertEscalationNotification,
            NotificationType.AlertAssignment => AlertAssignmentNotification,
            NotificationType.AlertResolution => AlertResolutionNotification,
            NotificationType.HighSeverityAttack => HighSeverityAttackNotification,
            NotificationType.MalwareDetection => MalwareDetectionNotification,
            NotificationType.BruteForce => BruteForceNotification,
            NotificationType.NewThreatActor => NewThreatActorNotification,
            NotificationType.ThreatLevelEscalation => ThreatLevelEscalationNotification,
            NotificationType.HoneypotOffline => HoneypotOfflineNotification,
            NotificationType.HoneypotHealth => HoneypotHealthNotification,
            NotificationType.StorageWarning => StorageWarningNotification,
            NotificationType.QuotaWarning => QuotaWarningNotification,
            NotificationType.SubscriptionExpiring => SubscriptionExpiringNotification,
            NotificationType.Maintenance => MaintenanceNotification,
            NotificationType.ProductUpdate => ProductUpdatesEnabled,
            NotificationType.SecurityAdvisory => SecurityAdvisoriesEnabled,
            NotificationType.TipsAndPractices => TipsAndBestPracticesEnabled,
            NotificationType.WeeklySummary => WeeklySummaryEnabled,
            NotificationType.MonthlySummary => MonthlySummaryEnabled,
            _ => true
        };
    }

    /// <summary>
    /// Get enabled channels for notification.
    /// </summary>
    public List<NotificationChannel> GetEnabledChannels(bool isCritical = false)
    {
        var channels = new List<NotificationChannel>();

        if (InAppNotificationsEnabled)
            channels.Add(NotificationChannel.InApp);

        if (EmailNotificationsEnabled)
            channels.Add(NotificationChannel.Email);

        if (PushNotificationsEnabled)
            channels.Add(NotificationChannel.Push);

        // SMS only for critical if enabled
        if (SmsNotificationsEnabled && isCritical)
            channels.Add(NotificationChannel.Sms);

        return channels;
    }

    #endregion
}

/// <summary>
/// Minimum severity threshold for alert notifications.
/// </summary>
public enum AlertSeverityThreshold
{
    /// <summary>Receive all alerts.</summary>
    Info = 0,
    
    /// <summary>Low and above.</summary>
    Low = 1,
    
    /// <summary>Medium and above.</summary>
    Medium = 2,
    
    /// <summary>High and above.</summary>
    High = 3,
    
    /// <summary>Critical only.</summary>
    Critical = 4
}

/// <summary>
/// Digest frequency for notifications.
/// </summary>
public enum DigestFrequency
{
    /// <summary>Send immediately.</summary>
    Immediate = 0,
    
    /// <summary>Batch every hour.</summary>
    Hourly = 1,
    
    /// <summary>Daily digest.</summary>
    Daily = 2,
    
    /// <summary>Weekly digest.</summary>
    Weekly = 3
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    // Alert notifications
    AlertCreated = 0,
    AlertEscalation = 1,
    AlertAssignment = 2,
    AlertResolution = 3,
    
    // Attack notifications
    HighSeverityAttack = 10,
    MalwareDetection = 11,
    BruteForce = 12,
    
    // Threat actor notifications
    NewThreatActor = 20,
    ThreatLevelEscalation = 21,
    
    // Honeypot notifications
    HoneypotOffline = 30,
    HoneypotHealth = 31,
    StorageWarning = 32,
    
    // System notifications
    QuotaWarning = 40,
    SubscriptionExpiring = 41,
    Maintenance = 42,
    
    // Communication
    ProductUpdate = 50,
    SecurityAdvisory = 51,
    TipsAndPractices = 52,
    
    // Reports
    WeeklySummary = 60,
    MonthlySummary = 61
}

/// <summary>
/// Notification delivery channels.
/// </summary>
public enum NotificationChannel
{
    /// <summary>In-app notification (dashboard).</summary>
    InApp = 0,
    
    /// <summary>Email notification.</summary>
    Email = 1,
    
    /// <summary>SMS notification.</summary>
    Sms = 2,
    
    /// <summary>Push notification (browser/mobile).</summary>
    Push = 3,
    
    /// <summary>Slack integration.</summary>
    Slack = 4,
    
    /// <summary>Microsoft Teams integration.</summary>
    Teams = 5
}
