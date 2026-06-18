using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Domain.Identity;

/// <summary>
/// Repository interface for two-factor authentication backup code operations.
/// </summary>
public interface ITwoFactorBackupCodeRepository
{
    /// <summary>
    /// Adds a new backup code.
    /// </summary>
    Task AddAsync(TwoFactorBackupCode code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple backup codes in a batch.
    /// </summary>
    Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unused backup codes for a user.
    /// </summary>
    Task<IReadOnlyList<TwoFactorBackupCode>> GetUnusedCodesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unused backup codes for a user.
    /// </summary>
    Task<int> GetUnusedCodeCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a backup code by its hash for a specific user.
    /// Used for code validation.
    /// </summary>
    Task<TwoFactorBackupCode?> FindByCodeHashAsync(
        Guid userId,
        string codeHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a backup code (marks as used).
    /// </summary>
    void Update(TwoFactorBackupCode code);

    /// <summary>
    /// Deletes all backup codes for a user.
    /// Used when regenerating codes or disabling 2FA.
    /// </summary>
    Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all used backup codes older than the specified date.
    /// Used for cleanup.
    /// </summary>
    Task<int> DeleteUsedCodesOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
