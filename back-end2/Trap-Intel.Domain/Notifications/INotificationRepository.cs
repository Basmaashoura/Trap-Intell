using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Domain.Notifications;

/// <summary>
/// Domain repository interface for managing inbox notifications.
/// All underlying persistence code (EF Core configs) belong to Infrastructure Layer.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUnreadNotificationsAsync(Guid userId, int maxCount = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> SearchUserNotificationsAsync(
        Guid userId,
        bool unreadOnly,
        NotificationCategory? category,
        NotificationPriority? priority,
        string? search,
        string? sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<UserPushToken>> GetUserPushTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPushToken?> GetPushTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddPushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default);
    Task DeletePushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default);
    Task UpdatePushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default);
    Task ArchiveAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
