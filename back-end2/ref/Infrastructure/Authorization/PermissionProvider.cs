using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Authorization;

/// <summary>
/// Provides user permissions from the database with caching support.
/// Implements secure permission retrieval with:
/// - Memory caching for performance
/// - Cache invalidation support
/// - Comprehensive logging
/// </summary>
internal sealed class PermissionProvider
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionProvider> _logger;

    /// <summary>
    /// Cache duration for permissions.
    /// Short enough to reflect permission changes reasonably quickly,
    /// long enough to reduce database load.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache key prefix for permission entries.
    /// </summary>
    private const string CacheKeyPrefix = "user_permissions_";

    public PermissionProvider(
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<PermissionProvider> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all permissions for a user by their domain ID.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="userId">The domain user ID.</param>
    /// <returns>Set of permission codes the user has access to.</returns>
    public async Task<HashSet<string>> GetForUserIdAsync(Guid userId)
    {
        // Validate input
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetForUserIdAsync called with empty GUID");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        string cacheKey = GetCacheKey(userId);

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPermissions) &&
            cachedPermissions is not null)
        {
            _logger.LogDebug(
                "Cache hit for user {UserId} permissions ({Count} permissions)",
                userId,
                cachedPermissions.Count);
            return cachedPermissions;
        }

        // Cache miss - fetch from database
        _logger.LogDebug("Cache miss for user {UserId} permissions, fetching from database", userId);

        HashSet<string> permissions = await FetchPermissionsFromDatabaseAsync(userId);

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Priority = CacheItemPriority.Normal,
            Size = permissions.Count + 1 // Approximate size for memory management
        };

        _cache.Set(cacheKey, permissions, cacheOptions);

        _logger.LogDebug(
            "Cached {Count} permissions for user {UserId}",
            permissions.Count,
            userId);

        return permissions;
    }

    /// <summary>
    /// Invalidates the permission cache for a specific user.
    /// Call this when user roles or permissions change.
    /// </summary>
    /// <param name="userId">The domain user ID.</param>
    public void InvalidateCache(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        string cacheKey = GetCacheKey(userId);
        _cache.Remove(cacheKey);

        _logger.LogInformation("Invalidated permission cache for user {UserId}", userId);
    }

    /// <summary>
    /// Invalidates the permission cache for multiple users.
    /// Useful when a role's permissions are modified.
    /// </summary>
    /// <param name="userIds">The domain user IDs.</param>
    public void InvalidateCacheForUsers(IEnumerable<Guid> userIds)
    {
        foreach (Guid userId in userIds)
        {
            InvalidateCache(userId);
        }
    }

    /// <summary>
    /// Fetches permissions from the database.
    /// Query path: UserRoles -> Role (active) -> RolePermissions -> Permission
    /// </summary>
    private async Task<HashSet<string>> FetchPermissionsFromDatabaseAsync(Guid userId)
    {
        try
        {
            // Query through the relationship chain:
            // User -> UserRoles -> Role (must be active) -> RolePermissions -> Permission
            List<string> permissions = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Where(ur => ur.Role != null && ur.Role.IsActive) // Only active roles
                .SelectMany(ur => ur.Role!.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Code)
                .Distinct()
                .ToListAsync();

            _logger.LogDebug(
                "Fetched {Count} permissions from database for user {UserId}",
                permissions.Count,
                userId);

            // Use case-insensitive comparer for consistency
            return new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching permissions from database for user {UserId}",
                userId);

            // Return empty set on error - fail closed (deny by default)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Generates a consistent cache key for a user.
    /// </summary>
    private static string GetCacheKey(Guid userId)
    {
        return $"{CacheKeyPrefix}{userId:N}"; // N format: no dashes, lowercase
    }
}
