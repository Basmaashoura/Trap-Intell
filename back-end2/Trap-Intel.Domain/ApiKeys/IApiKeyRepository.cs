using Trap_Intel.Domain.ApiKeys.Enums;

namespace Trap_Intel.Domain.ApiKeys;

/// <summary>
/// Repository interface for ApiKey aggregate.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Get API key by ID.
    /// </summary>
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API key by key hash (for validation).
    /// </summary>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API key by key prefix (for lookup).
    /// </summary>
    Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all API keys for an organization.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetByOrganizationAsync(
        Guid organizationId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active API keys for an organization.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetActiveByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API keys by type for an organization.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetByTypeAsync(
        Guid organizationId,
        ApiKeyType keyType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API keys expiring soon.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetExpiringSoonAsync(
        int daysUntilExpiration = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired API keys that need status update.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetExpiredKeysNeedingUpdateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count API keys for an organization.
    /// </summary>
    Task<int> CountByOrganizationAsync(
        Guid organizationId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if API key name exists in organization.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        Guid organizationId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new API key.
    /// </summary>
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing API key.
    /// </summary>
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete API key (hard delete - use Revoke for soft delete).
    /// </summary>
    Task DeleteAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
}
