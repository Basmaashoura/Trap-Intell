using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
    }


    public async Task<Notification?> GetByIdWithTypeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Type)
            .Include(n => n.CreatedBy)
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetPaginatedAsync(
        Guid userId,
        int page,
        int pageSize,
        bool? isRead = null,
        IEnumerable<string>? typeCodes = null,
        IEnumerable<NotificationPriority>? priorities = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> query = _context.Notifications
            .Include(n => n.Type)
            .Include(n => n.CreatedBy)
            .Where(n => n.UserId == userId && !n.IsDismissed && !n.IsArchived)
            .AsNoTracking();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        if (typeCodes is not null)
        {
            var codes = typeCodes.ToList();
            if (codes.Count > 0)
            {
                query = query.Where(n => n.Type != null && codes.Contains(n.Type.Code));
            }
        }

        if (priorities is not null)
        {
            var priorityList = priorities.ToList();
            if (priorityList.Count > 0)
            {
                query = query.Where(n => priorityList.Contains(n.Priority));
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= fromDate.Value);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Notification> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Notification>> GetUnreadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDismissed)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetReadNotArchivedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead && !n.IsArchived && !n.IsDismissed)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetArchivedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> query = _context.Notifications
            .Include(n => n.Type)
            .Where(n => n.UserId == userId && n.IsArchived)
            .OrderByDescending(n => n.ArchivedAt)
            .AsNoTracking();

        int totalCount = await query.CountAsync(cancellationToken);

        List<Notification> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n =>
                n.UserId == userId &&
                !n.IsRead &&
                !n.IsDismissed &&
                !n.IsArchived,
                cancellationToken);
    }

    public async Task<List<Notification>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        return await _context.Notifications
            .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now && !n.IsDismissed)
            .ToListAsync(cancellationToken);
    }


    public async Task<List<Notification>> GetScheduledReadyToSendAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        return await _context.Notifications
            .Include(n => n.Type)
            .Where(n =>
                n.ScheduledFor.HasValue &&
                n.ScheduledFor.Value <= now &&
                !n.IsRead &&
                !n.IsDismissed &&
                !n.IsArchived &&
                n.Deliveries.Count == 0)
            .ToListAsync(cancellationToken);
    }

    public void Add(Notification notification)
    {
        _context.Notifications.Add(notification);
    }

    public void AddRange(IEnumerable<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }
}
