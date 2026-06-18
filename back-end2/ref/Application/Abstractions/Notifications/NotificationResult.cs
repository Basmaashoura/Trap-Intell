namespace Application.Abstractions.Notifications;

/// <summary>
/// Result of a notification send operation.
/// </summary>
public sealed class NotificationResult
{
    /// <summary>
    /// Whether the notification was sent successfully.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// IDs of notifications that were created.
    /// </summary>
    public List<Guid> NotificationIds { get; private set; } = [];

    /// <summary>
    /// Number of users who received the notification.
    /// </summary>
    public int RecipientCount { get; private set; }

    /// <summary>
    /// Error message if sending failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Detailed errors per recipient if some failed.
    /// </summary>
    public Dictionary<Guid, string> FailedRecipients { get; private set; } = [];

    private NotificationResult()
    {
    }

    public static NotificationResult Success(List<Guid> notificationIds, int recipientCount)
    {
        return new NotificationResult
        {
            IsSuccess = true,
            NotificationIds = notificationIds,
            RecipientCount = recipientCount
        };
    }

    public static NotificationResult PartialSuccess(
        List<Guid> notificationIds,
        int recipientCount,
        Dictionary<Guid, string> failedRecipients)
    {
        return new NotificationResult
        {
            IsSuccess = true,
            NotificationIds = notificationIds,
            RecipientCount = recipientCount,
            FailedRecipients = failedRecipients
        };
    }

    public static NotificationResult Failure(string errorMessage)
    {
        return new NotificationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    public static NotificationResult Skipped()
    {
        return new NotificationResult
        {
            IsSuccess = true,
            RecipientCount = 0
        };
    }
}
