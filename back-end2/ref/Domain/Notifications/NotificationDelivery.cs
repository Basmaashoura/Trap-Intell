using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Tracks the delivery of a notification through a specific channel.
/// </summary>
public sealed class NotificationDelivery : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The notification being delivered.
    /// </summary>
    public Guid NotificationId { get; private set; }

    /// <summary>
    /// The channel used for delivery.
    /// </summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>
    /// When the notification was sent.
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// When delivery was confirmed.
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// When the user clicked/interacted.
    /// </summary>
    public DateTime? ClickedAt { get; private set; }

    /// <summary>
    /// Reason for failure if delivery failed.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// When to retry next (if failed).
    /// </summary>
    public DateTime? NextRetryAt { get; private set; }

    /// <summary>
    /// External ID from the delivery provider (e.g., FCM message ID).
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// When this delivery record was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Notification? Notification { get; init; }

    private NotificationDelivery()
    {
    }

    public static NotificationDelivery Create(
        Guid notificationId,
        NotificationChannel channel)
    {
        return new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            Channel = channel,
            Status = DeliveryStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent(string? externalId = null)
    {
        Status = DeliveryStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalId = externalId;
    }

    public void MarkAsDelivered()
    {
        Status = DeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkAsClicked()
    {
        Status = DeliveryStatus.Clicked;
        ClickedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason, DateTime? nextRetryAt = null)
    {
        Status = DeliveryStatus.Failed;
        FailureReason = reason;
        RetryCount++;
        NextRetryAt = nextRetryAt;
    }

    public void MarkAsSkipped(string reason)
    {
        Status = DeliveryStatus.Skipped;
        FailureReason = reason;
    }

    public void ResetForRetry()
    {
        Status = DeliveryStatus.Pending;
        NextRetryAt = null;
    }
}
