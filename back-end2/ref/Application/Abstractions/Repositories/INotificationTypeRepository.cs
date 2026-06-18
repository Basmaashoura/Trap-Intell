using Domain.Notifications;

namespace Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for NotificationType entity operations.
/// </summary>
public interface INotificationTypeRepository
{
    /// <summary>
    /// Gets a notification type by ID.
    /// </summary>
    Task<NotificationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification type by code.
    /// </summary>
    Task<NotificationType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active notification types.
    /// </summary>
    Task<List<NotificationType>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active notification types by category.
    /// </summary>
    Task<List<NotificationType>> GetByCategoryAsync(
        NotificationCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a notification type exists by ID.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
