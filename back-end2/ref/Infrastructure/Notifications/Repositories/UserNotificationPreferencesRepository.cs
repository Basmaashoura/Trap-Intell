using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications.Repositories;

internal sealed class UserNotificationPreferencesRepository : IUserNotificationPreferencesRepository
{
    private readonly ApplicationDbContext _context;

    public UserNotificationPreferencesRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserNotificationPreferences?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserNotificationPreferences> GetOrCreateDefaultAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        UserNotificationPreferences? preferences = await GetByUserIdAsync(userId, cancellationToken);

        if (preferences is null)
        {
            preferences = UserNotificationPreferences.CreateDefault(userId);
            _context.UserNotificationPreferences.Add(preferences);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return preferences;
    }

    public async Task<List<UserNotificationTypeSetting>> GetTypeSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationTypeSettings
            .Include(s => s.Type)
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserNotificationTypeSetting?> GetTypeSettingAsync(
        Guid userId,
        Guid typeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationTypeSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TypeId == typeId, cancellationToken);
    }

    public void Add(UserNotificationPreferences preferences)
    {
        _context.UserNotificationPreferences.Add(preferences);
    }

    public void AddTypeSetting(UserNotificationTypeSetting setting)
    {
        _context.UserNotificationTypeSettings.Add(setting);
    }
}
