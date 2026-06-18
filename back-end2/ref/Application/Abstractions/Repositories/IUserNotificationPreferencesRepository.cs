using Domain.Notifications;

namespace Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for UserNotificationPreferences and related entities.
/// </summary>
public interface IUserNotificationPreferencesRepository
{
    /// <summary>
    /// Gets user notification preferences.
    /// </summary>
    Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates default preferences for a user.
    /// </summary>
    Task<UserNotificationPreferences> GetOrCreateDefaultAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets type settings for a user.
    /// </summary>
    Task<List<UserNotificationTypeSetting>> GetTypeSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific type setting for a user.
    /// </summary>
    Task<UserNotificationTypeSetting?> GetTypeSettingAsync(
        Guid userId,
        Guid typeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new preferences record.
    /// </summary>
    void Add(UserNotificationPreferences preferences);

    /// <summary>
    /// Adds a new type setting.
    /// </summary>
    void AddTypeSetting(UserNotificationTypeSetting setting);
}
