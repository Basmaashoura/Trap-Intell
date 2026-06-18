using Domain.Notifications;

namespace Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for Notification entity operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by ID for a specific user.
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification by ID with its type.
    /// </summary>
    Task<Notification?> GetByIdWithTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated notifications for a user.
    /// </summary>
    Task<(List<Notification> Items, int TotalCount)> GetPaginatedAsync(
        Guid userId,
        int page,
        int pageSize,
        bool? isRead = null,
        IEnumerable<string>? typeCodes = null,
        IEnumerable<NotificationPriority>? priorities = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a user.
    /// </summary>
    Task<List<Notification>> GetUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets read notifications for a user (for archiving).
    /// </summary>
    Task<List<Notification>> GetReadNotArchivedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets archived notifications for a user.
    /// </summary>
    Task<(List<Notification> Items, int TotalCount)> GetArchivedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired notifications that need cleanup.
    /// </summary>
    Task<List<Notification>> GetExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled notifications that are ready to be sent.
    /// </summary>
    Task<List<Notification>> GetScheduledReadyToSendAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification.
    /// </summary>
    void Add(Notification notification);

    /// <summary>
    /// Adds multiple notifications.
    /// </summary>
    void AddRange(IEnumerable<Notification> notifications);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    void Update(Notification notification);
}
