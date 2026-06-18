using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications.Repositories;

internal sealed class NotificationTypeRepository : INotificationTypeRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationType?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<NotificationType?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTypes
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive, cancellationToken);
    }

    public async Task<List<NotificationType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationType>> GetByCategoryAsync(
        NotificationCategory category,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTypes
            .Where(t => t.IsActive && t.Category == category)
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTypes
            .AnyAsync(t => t.Id == id, cancellationToken);
    }
}
