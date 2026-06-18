using Domain.Notifications;

namespace Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for UserPushToken entity operations.
/// </summary>
public interface IUserPushTokenRepository
{
    /// <summary>
    /// Gets a push token by ID.
    /// </summary>
    Task<UserPushToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a push token by its value.
    /// </summary>
    Task<UserPushToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active push tokens for a user.
    /// </summary>
    Task<List<UserPushToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new push token.
    /// </summary>
    void Add(UserPushToken token);

    /// <summary>
    /// Removes a push token.
    /// </summary>
    void Remove(UserPushToken token);
}
