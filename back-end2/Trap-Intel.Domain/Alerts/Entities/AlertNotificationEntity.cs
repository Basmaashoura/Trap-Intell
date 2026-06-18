using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Alerts.Entities;

/// <summary>
/// Represents a notification sent for an alert.
/// Child entity owned by Alert aggregate.
/// Tracks delivery status, retries, and confirmation.
/// </summary>
public class AlertNotificationEntity : Entity<Guid>
{
    private List<string> _recipients = new();

    // Private constructor for EF
    private AlertNotificationEntity() { }

    private AlertNotificationEntity(
        Guid id,
        Guid alertId,
        NotificationChannel channel,
        NotificationTrigger trigger,
        List<string> recipients)
        : base(id)
    {
        AlertId = alertId;
        Channel = channel;
        Trigger = trigger;
        _recipients = recipients ?? new();
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    #region Properties

    /// <summary>
    /// Parent alert ID.
    /// </summary>
    public Guid AlertId { get; private set; }

    /// <summary>
    /// Notification channel used.
    /// </summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>
    /// What triggered this notification.
    /// </summary>
    public NotificationTrigger Trigger { get; private set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// List of recipients (emails, phone numbers, user IDs, etc.)
    /// </summary>
    public IReadOnlyList<string> Recipients => _recipients.AsReadOnly();

    /// <summary>
    /// When notification was created/queued.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When notification was actually sent.
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// When delivery was confirmed.
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// When notification failed (if failed).
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Maximum retries allowed.
    /// </summary>
    public int MaxRetries { get; private set; } = 3;

    /// <summary>
    /// Failure reason (if failed).
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// External message ID from provider (e.g., SendGrid, Twilio).
    /// </summary>
    public string? ExternalMessageId { get; private set; }

    /// <summary>
    /// Provider-specific response data.
    /// </summary>
    public string? ProviderResponse { get; private set; }

    /// <summary>
    /// Notification content/subject.
    /// </summary>
    public string? Subject { get; private set; }

    /// <summary>
    /// Notification body preview.
    /// </summary>
    public string? BodyPreview { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new notification.
    /// </summary>
    public static Result<AlertNotificationEntity> Create(
        Guid alertId,
        NotificationChannel channel,
        NotificationTrigger trigger,
        List<string> recipients,
        string? subject = null,
        string? bodyPreview = null)
    {
        if (alertId == Guid.Empty)
            return Result.Failure<AlertNotificationEntity>(AlertErrors.InvalidAlertId);

        if (recipients == null || recipients.Count == 0)
            return Result.Failure<AlertNotificationEntity>(AlertErrors.NoNotificationRecipients);

        var notification = new AlertNotificationEntity(
            Guid.NewGuid(),
            alertId,
            channel,
            trigger,
            recipients)
        {
            Subject = subject,
            BodyPreview = bodyPreview?.Length > 500 ? bodyPreview[..500] : bodyPreview
        };

        return Result.Success(notification);
    }

    /// <summary>
    /// Create notification for alert creation.
    /// </summary>
    public static Result<AlertNotificationEntity> ForAlertCreated(
        Guid alertId,
        NotificationChannel channel,
        List<string> recipients)
    {
        return Create(alertId, channel, NotificationTrigger.AlertCreated, recipients);
    }

    /// <summary>
    /// Create notification for escalation.
    /// </summary>
    public static Result<AlertNotificationEntity> ForEscalation(
        Guid alertId,
        NotificationChannel channel,
        List<string> recipients)
    {
        return Create(alertId, channel, NotificationTrigger.Escalation, recipients);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static AlertNotificationEntity Reconstruct(
        Guid id,
        Guid alertId,
        NotificationChannel channel,
        NotificationTrigger trigger,
        NotificationStatus status,
        List<string> recipients,
        DateTime createdAt,
        DateTime? sentAt,
        DateTime? deliveredAt,
        DateTime? failedAt,
        int retryCount,
        int maxRetries,
        string? failureReason,
        string? externalMessageId,
        string? providerResponse,
        string? subject,
        string? bodyPreview)
    {
        return new AlertNotificationEntity
        {
            Id = id,
            AlertId = alertId,
            Channel = channel,
            Trigger = trigger,
            Status = status,
            _recipients = recipients ?? new(),
            CreatedAt = createdAt,
            SentAt = sentAt,
            DeliveredAt = deliveredAt,
            FailedAt = failedAt,
            RetryCount = retryCount,
            MaxRetries = maxRetries,
            FailureReason = failureReason,
            ExternalMessageId = externalMessageId,
            ProviderResponse = providerResponse,
            Subject = subject,
            BodyPreview = bodyPreview
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Mark notification as sent.
    /// </summary>
    public Result MarkAsSent(string? externalMessageId = null)
    {
        if (Status == NotificationStatus.Delivered)
            return Result.Success(); // Already delivered

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalMessageId = externalMessageId;

        return Result.Success();
    }

    /// <summary>
    /// Mark notification as delivered (confirmed).
    /// </summary>
    public Result MarkAsDelivered(string? providerResponse = null)
    {
        if (Status == NotificationStatus.Delivered)
            return Result.Success();

        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        ProviderResponse = providerResponse;

        return Result.Success();
    }

    /// <summary>
    /// Mark notification as failed.
    /// </summary>
    public Result MarkAsFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(AlertErrors.InvalidFailureReason);

        Status = NotificationStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;

        return Result.Success();
    }

    /// <summary>
    /// Record a retry attempt.
    /// </summary>
    public Result RecordRetry()
    {
        if (RetryCount >= MaxRetries)
            return Result.Failure(AlertErrors.MaxRetriesExceeded);

        RetryCount++;
        Status = NotificationStatus.Retrying;

        return Result.Success();
    }

    /// <summary>
    /// Check if can retry.
    /// </summary>
    public bool CanRetry() => RetryCount < MaxRetries && 
                               (Status == NotificationStatus.Failed || Status == NotificationStatus.Retrying);

    /// <summary>
    /// Cancel pending notification.
    /// </summary>
    public Result Cancel(string reason)
    {
        if (Status != NotificationStatus.Pending)
            return Result.Failure(AlertErrors.CannotCancelSentNotification);

        Status = NotificationStatus.Cancelled;
        FailureReason = reason;

        return Result.Success();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if notification was successful.
    /// </summary>
    public bool IsSuccessful() => Status == NotificationStatus.Sent || Status == NotificationStatus.Delivered;

    /// <summary>
    /// Get time to delivery (if delivered).
    /// </summary>
    public TimeSpan? GetTimeToDelivery() => DeliveredAt.HasValue 
        ? DeliveredAt.Value - CreatedAt 
        : null;

    /// <summary>
    /// Check if notification is pending.
    /// </summary>
    public bool IsPending() => Status == NotificationStatus.Pending || Status == NotificationStatus.Retrying;

    #endregion
}

/// <summary>
/// What triggered the notification.
/// </summary>
public enum NotificationTrigger
{
    AlertCreated = 0,
    Escalation = 1,
    Assignment = 2,
    Comment = 3,
    Resolution = 4,
    SeverityChange = 5,
    SnoozeExpired = 6,
    Reminder = 7,
    SLABreach = 8,
    Custom = 99
}

/// <summary>
/// Notification delivery status.
/// </summary>
public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Retrying = 4,
    Cancelled = 5,
    Bounced = 6,
    Clicked = 7    // For email tracking
}
