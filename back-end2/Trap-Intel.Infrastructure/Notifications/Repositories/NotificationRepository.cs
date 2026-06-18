using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Notifications.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadNotificationsAsync(Guid userId, int maxCount = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDismissed)
            .OrderByDescending(n => n.CreatedAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsDismissed)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> SearchUserNotificationsAsync(
        Guid userId,
        bool unreadOnly,
        NotificationCategory? category,
        NotificationPriority? priority,
        string? search,
        string? sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDismissed);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (category.HasValue)
        {
            query = query.Where(n => n.Category == category.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(n => n.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";

            query = query.Where(n =>
                EF.Functions.ILike(n.Title, pattern) ||
                EF.Functions.ILike(n.Message, pattern) ||
                EF.Functions.ILike(n.Type, pattern));
        }

        query = ApplySort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDismissed, cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDismissed)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        _dbContext.Notifications.UpdateRange(unreadNotifications);
    }

    public async Task<List<UserPushToken>> GetUserPushTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPushTokens
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPushToken?> GetPushTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task AddPushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserPushTokens.AddAsync(token, cancellationToken);
    }

    public Task DeletePushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.UserPushTokens.Remove(token);
        return Task.CompletedTask;
    }

    public Task UpdatePushTokenAsync(UserPushToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.UserPushTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task ArchiveAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var readNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && n.IsRead && !n.IsDismissed)
            .ToListAsync(cancellationToken);

        foreach (var notification in readNotifications)
        {
            notification.Dismiss();  // Archiving/Dismissing
        }

        _dbContext.Notifications.UpdateRange(readNotifications);
    }

    private static IQueryable<Notification> ApplySort(
        IQueryable<Notification> query,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "createdat" or "created" => descending
                ? query.OrderByDescending(n => n.CreatedAt)
                : query.OrderBy(n => n.CreatedAt),

            "readat" => descending
                ? query.OrderByDescending(n => n.ReadAt).ThenByDescending(n => n.CreatedAt)
                : query.OrderBy(n => n.ReadAt).ThenBy(n => n.CreatedAt),

            "priority" => descending
                ? query.OrderByDescending(n => n.Priority).ThenByDescending(n => n.CreatedAt)
                : query.OrderBy(n => n.Priority).ThenBy(n => n.CreatedAt),

            "category" => descending
                ? query.OrderByDescending(n => n.Category).ThenByDescending(n => n.CreatedAt)
                : query.OrderBy(n => n.Category).ThenBy(n => n.CreatedAt),

            "title" => descending
                ? query.OrderByDescending(n => n.Title).ThenByDescending(n => n.CreatedAt)
                : query.OrderBy(n => n.Title).ThenBy(n => n.CreatedAt),

            _ => query.OrderByDescending(n => n.CreatedAt)
        };
    }
}
