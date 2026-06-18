using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.ApiKeys.Enums;
using Trap_Intel.Domain.ApiKeys.Events;
using Trap_Intel.Domain.ApiKeys.ValueObjects;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.ApiKeys;

/// <summary>
/// Represents an API key for programmatic access to the platform.
/// Enables organizations to integrate with external systems and automate workflows.
/// Supports scoped permissions, rate limiting, and full lifecycle management.
/// </summary>
public class ApiKey : AggregateRoot<Guid>
{
    private List<ApiKeyPermission> _permissions = new();
    private List<ApiKeyUsageRecord> _recentUsage = new();

    // Private constructor for EF
    private ApiKey() { }

    private ApiKey(
        Guid id,
        Guid organizationId,
        string name,
        string keyPrefix,
        string keyHash,
        Guid createdByUserId)
        : base(id)
    {
        OrganizationId = organizationId;
        Name = name;
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        CreatedByUserId = createdByUserId;
        Status = ApiKeyStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version = 1;
    }

    #region Properties

    /// <summary>
    /// Organization that owns this API key.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Human-readable name for the API key.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of what this key is used for.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// First 8 characters of the key for identification (e.g., "ti_live_").
    /// </summary>
    public string KeyPrefix { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the full API key (never store the raw key).
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the API key.
    /// </summary>
    public ApiKeyStatus Status { get; private set; }

    /// <summary>
    /// Type of API key (Live, Test, ReadOnly).
    /// </summary>
    public ApiKeyType KeyType { get; private set; } = ApiKeyType.Live;

    /// <summary>
    /// When the key expires (null = never).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// When the key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// IP address from which key was last used.
    /// </summary>
    public string? LastUsedFromIP { get; private set; }

    /// <summary>
    /// Total number of API calls made with this key.
    /// </summary>
    public long TotalUsageCount { get; private set; }

    /// <summary>
    /// Rate limit configuration.
    /// </summary>
    public ApiKeyRateLimit RateLimit { get; private set; } = ApiKeyRateLimit.Default();

    /// <summary>
    /// Current rate limit window usage.
    /// </summary>
    public int CurrentWindowUsage { get; private set; }

    /// <summary>
    /// When the current rate limit window started.
    /// </summary>
    public DateTime? RateLimitWindowStart { get; private set; }

    /// <summary>
    /// IP whitelist (empty = allow all).
    /// </summary>
    public List<string> AllowedIPs { get; private set; } = new();

    /// <summary>
    /// User who created this key.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// User who revoked this key (if revoked).
    /// </summary>
    public Guid? RevokedByUserId { get; private set; }

    /// <summary>
    /// When the key was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// When key was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When key was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Version number (incremented on rotation).
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Permissions granted to this key.
    /// </summary>
    public IReadOnlyList<ApiKeyPermission> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Recent usage records (last 100).
    /// </summary>
    public IReadOnlyList<ApiKeyUsageRecord> RecentUsage => _recentUsage.AsReadOnly();

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new API key.
    /// Returns the key entity AND the raw key (only returned once, at creation).
    /// </summary>
    public static Result<(ApiKey ApiKey, string RawKey)> Create(
        Guid organizationId,
        string name,
        Guid createdByUserId,
        ApiKeyType keyType = ApiKeyType.Live,
        string? description = null,
        DateTime? expiresAt = null,
        List<ApiKeyPermission>? permissions = null,
        ApiKeyRateLimit? rateLimit = null,
        List<string>? allowedIPs = null)
    {
        // Validation
        if (organizationId == Guid.Empty)
            return Result.Failure<(ApiKey, string)>(ApiKeyErrors.InvalidOrganizationId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<(ApiKey, string)>(ApiKeyErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure<(ApiKey, string)>(ApiKeyErrors.NameTooLong);

        if (createdByUserId == Guid.Empty)
            return Result.Failure<(ApiKey, string)>(ApiKeyErrors.InvalidUserId);

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            return Result.Failure<(ApiKey, string)>(ApiKeyErrors.ExpirationInPast);

        // Generate secure key
        var (rawKey, keyPrefix, keyHash) = GenerateSecureKey(keyType);

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            organizationId,
            name.Trim(),
            keyPrefix,
            keyHash,
            createdByUserId)
        {
            KeyType = keyType,
            Description = description?.Trim(),
            ExpiresAt = expiresAt,
            RateLimit = rateLimit ?? ApiKeyRateLimit.Default(),
            AllowedIPs = allowedIPs ?? new(),
            _permissions = permissions ?? new List<ApiKeyPermission>
            {
                ApiKeyPermission.ReadHoneypots,
                ApiKeyPermission.ReadAttacks,
                ApiKeyPermission.ReadAlerts
            }
        };

        apiKey.RaiseDomainEvent(new ApiKeyCreatedEvent(
            apiKey.Id,
            organizationId,
            name,
            keyType,
            keyPrefix,
            createdByUserId,
            expiresAt,
            DateTime.UtcNow));

        return Result.Success((apiKey, rawKey));
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static ApiKey Reconstruct(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        string keyPrefix,
        string keyHash,
        ApiKeyStatus status,
        ApiKeyType keyType,
        DateTime? expiresAt,
        DateTime? lastUsedAt,
        string? lastUsedFromIP,
        long totalUsageCount,
        ApiKeyRateLimit rateLimit,
        int currentWindowUsage,
        DateTime? rateLimitWindowStart,
        List<string> allowedIPs,
        Guid createdByUserId,
        Guid? revokedByUserId,
        DateTime? revokedAt,
        string? revocationReason,
        DateTime createdAt,
        DateTime updatedAt,
        int version,
        List<ApiKeyPermission>? permissions = null,
        List<ApiKeyUsageRecord>? recentUsage = null)
    {
        return new ApiKey
        {
            Id = id,
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            Status = status,
            KeyType = keyType,
            ExpiresAt = expiresAt,
            LastUsedAt = lastUsedAt,
            LastUsedFromIP = lastUsedFromIP,
            TotalUsageCount = totalUsageCount,
            RateLimit = rateLimit,
            CurrentWindowUsage = currentWindowUsage,
            RateLimitWindowStart = rateLimitWindowStart,
            AllowedIPs = allowedIPs,
            CreatedByUserId = createdByUserId,
            RevokedByUserId = revokedByUserId,
            RevokedAt = revokedAt,
            RevocationReason = revocationReason,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Version = version,
            _permissions = permissions ?? new(),
            _recentUsage = recentUsage ?? new()
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Validate and record API key usage.
    /// Returns error if key is invalid, expired, rate limited, or IP restricted.
    /// </summary>
    public Result RecordUsage(string ipAddress, string endpoint)
    {
        // Check status
        if (Status != ApiKeyStatus.Active)
            return Result.Failure(ApiKeyErrors.KeyNotActive);

        // Check expiration
        if (IsExpired)
            return Result.Failure(ApiKeyErrors.KeyExpired);

        // Check IP whitelist
        if (AllowedIPs.Count > 0 && !AllowedIPs.Contains(ipAddress))
            return Result.Failure(ApiKeyErrors.IPNotAllowed);

        // Check rate limit
        if (IsRateLimited())
            return Result.Failure(ApiKeyErrors.RateLimitExceeded);

        // Record usage
        LastUsedAt = DateTime.UtcNow;
        LastUsedFromIP = ipAddress;
        TotalUsageCount++;
        IncrementRateLimitCounter();
        UpdatedAt = DateTime.UtcNow;

        // Add to recent usage (keep last 100)
        _recentUsage.Add(new ApiKeyUsageRecord(
            DateTime.UtcNow,
            ipAddress,
            endpoint,
            true,
            null));

        if (_recentUsage.Count > 100)
        {
            _recentUsage.RemoveAt(0);
        }

        RaiseDomainEvent(new ApiKeyUsedEvent(
            Id,
            OrganizationId,
            ipAddress,
            endpoint,
            TotalUsageCount,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Record failed usage attempt.
    /// </summary>
    public void RecordFailedUsage(string ipAddress, string endpoint, string reason)
    {
        _recentUsage.Add(new ApiKeyUsageRecord(
            DateTime.UtcNow,
            ipAddress,
            endpoint,
            false,
            reason));

        if (_recentUsage.Count > 100)
        {
            _recentUsage.RemoveAt(0);
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyUsageFailedEvent(
            Id,
            OrganizationId,
            ipAddress,
            endpoint,
            reason,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Revoke the API key.
    /// </summary>
    public Result Revoke(Guid revokedByUserId, string reason)
    {
        if (revokedByUserId == Guid.Empty)
            return Result.Failure(ApiKeyErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ApiKeyErrors.InvalidRevocationReason);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.AlreadyRevoked);

        Status = ApiKeyStatus.Revoked;
        RevokedByUserId = revokedByUserId;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyRevokedEvent(
            Id,
            OrganizationId,
            revokedByUserId,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Rotate the API key (generate new key, invalidate old).
    /// Returns the new raw key (only returned once).
    /// </summary>
    public Result<string> Rotate(Guid rotatedByUserId)
    {
        if (rotatedByUserId == Guid.Empty)
            return Result.Failure<string>(ApiKeyErrors.InvalidUserId);

        if (Status != ApiKeyStatus.Active)
            return Result.Failure<string>(ApiKeyErrors.CannotRotateInactiveKey);

        var oldKeyPrefix = KeyPrefix;
        var (rawKey, keyPrefix, keyHash) = GenerateSecureKey(KeyType);

        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        Version++;
        UpdatedAt = DateTime.UtcNow;

        // Reset rate limiting on rotation
        CurrentWindowUsage = 0;
        RateLimitWindowStart = null;

        RaiseDomainEvent(new ApiKeyRotatedEvent(
            Id,
            OrganizationId,
            oldKeyPrefix,
            keyPrefix,
            Version,
            rotatedByUserId,
            DateTime.UtcNow));

        return Result.Success(rawKey);
    }

    /// <summary>
    /// Suspend the API key temporarily.
    /// </summary>
    public Result Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ApiKeyErrors.InvalidReason);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotSuspendRevokedKey);

        if (Status == ApiKeyStatus.Suspended)
            return Result.Success(); // Already suspended

        var oldStatus = Status;
        Status = ApiKeyStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyStatusChangedEvent(
            Id,
            OrganizationId,
            oldStatus,
            ApiKeyStatus.Suspended,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Reactivate a suspended API key.
    /// </summary>
    public Result Reactivate()
    {
        if (Status != ApiKeyStatus.Suspended)
            return Result.Failure(ApiKeyErrors.CannotReactivateNonSuspendedKey);

        if (IsExpired)
            return Result.Failure(ApiKeyErrors.CannotReactivateExpiredKey);

        Status = ApiKeyStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyStatusChangedEvent(
            Id,
            OrganizationId,
            ApiKeyStatus.Suspended,
            ApiKeyStatus.Active,
            "Reactivated",
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update API key permissions.
    /// </summary>
    public Result UpdatePermissions(List<ApiKeyPermission> newPermissions, Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
            return Result.Failure(ApiKeyErrors.InvalidUserId);

        if (newPermissions == null || newPermissions.Count == 0)
            return Result.Failure(ApiKeyErrors.NoPermissionsSpecified);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotUpdateRevokedKey);

        var oldPermissions = _permissions.ToList();
        _permissions = newPermissions.Distinct().ToList();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyPermissionsUpdatedEvent(
            Id,
            OrganizationId,
            oldPermissions,
            _permissions,
            updatedByUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update rate limit configuration.
    /// </summary>
    public Result UpdateRateLimit(ApiKeyRateLimit newRateLimit)
    {
        if (newRateLimit == null)
            return Result.Failure(ApiKeyErrors.InvalidRateLimit);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotUpdateRevokedKey);

        RateLimit = newRateLimit;
        CurrentWindowUsage = 0;
        RateLimitWindowStart = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyRateLimitUpdatedEvent(
            Id,
            OrganizationId,
            newRateLimit.RequestsPerMinute,
            newRateLimit.RequestsPerHour,
            newRateLimit.RequestsPerDay,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update IP whitelist.
    /// </summary>
    public Result UpdateAllowedIPs(List<string> newAllowedIPs)
    {
        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotUpdateRevokedKey);

        AllowedIPs = newAllowedIPs ?? new();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update name and description.
    /// </summary>
    public Result UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ApiKeyErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure(ApiKeyErrors.NameTooLong);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotUpdateRevokedKey);

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Extend expiration date.
    /// </summary>
    public Result ExtendExpiration(DateTime newExpiresAt)
    {
        if (newExpiresAt <= DateTime.UtcNow)
            return Result.Failure(ApiKeyErrors.ExpirationInPast);

        if (Status == ApiKeyStatus.Revoked)
            return Result.Failure(ApiKeyErrors.CannotUpdateRevokedKey);

        ExpiresAt = newExpiresAt;
        
        // If key was expired and now has future expiration, reactivate
        if (Status == ApiKeyStatus.Expired)
        {
            Status = ApiKeyStatus.Active;
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApiKeyExpirationExtendedEvent(
            Id,
            OrganizationId,
            newExpiresAt,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Check if key has specific permission.
    /// </summary>
    public bool HasPermission(ApiKeyPermission permission)
    {
        return _permissions.Contains(permission) || _permissions.Contains(ApiKeyPermission.FullAccess);
    }

    /// <summary>
    /// Validate API key hash matches.
    /// </summary>
    public bool ValidateKey(string rawKey)
    {
        var hash = ComputeKeyHash(rawKey);
        return hash == KeyHash;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if key is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Check if key is usable (active and not expired).
    /// </summary>
    public bool IsUsable => Status == ApiKeyStatus.Active && !IsExpired;

    /// <summary>
    /// Check if key is expiring soon (within 30 days).
    /// </summary>
    public bool IsExpiringSoon(int warningDays = 30)
    {
        if (!ExpiresAt.HasValue) return false;
        var warningDate = ExpiresAt.Value.AddDays(-warningDays);
        return DateTime.UtcNow >= warningDate && !IsExpired;
    }

    /// <summary>
    /// Get days until expiration.
    /// </summary>
    public int? GetDaysUntilExpiration()
    {
        if (!ExpiresAt.HasValue) return null;
        var days = (ExpiresAt.Value - DateTime.UtcNow).Days;
        return days > 0 ? days : 0;
    }

    /// <summary>
    /// Check if rate limited.
    /// </summary>
    public bool IsRateLimited()
    {
        if (RateLimitWindowStart == null)
            return false;

        var windowElapsed = DateTime.UtcNow - RateLimitWindowStart.Value;

        // Check per-minute limit
        if (windowElapsed.TotalMinutes < 1 && CurrentWindowUsage >= RateLimit.RequestsPerMinute)
            return true;

        return false;
    }

    /// <summary>
    /// Get remaining requests in current window.
    /// </summary>
    public int GetRemainingRequests()
    {
        if (RateLimitWindowStart == null)
            return RateLimit.RequestsPerMinute;

        var windowElapsed = DateTime.UtcNow - RateLimitWindowStart.Value;
        
        if (windowElapsed.TotalMinutes >= 1)
            return RateLimit.RequestsPerMinute;

        return Math.Max(0, RateLimit.RequestsPerMinute - CurrentWindowUsage);
    }

    #endregion

    #region Private Methods

    private void IncrementRateLimitCounter()
    {
        var now = DateTime.UtcNow;

        // Check if we need to reset the window
        if (RateLimitWindowStart == null || (now - RateLimitWindowStart.Value).TotalMinutes >= 1)
        {
            RateLimitWindowStart = now;
            CurrentWindowUsage = 1;
        }
        else
        {
            CurrentWindowUsage++;
        }
    }

    private static (string RawKey, string KeyPrefix, string KeyHash) GenerateSecureKey(ApiKeyType keyType)
    {
        // Generate 32 bytes of random data
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);

        // Create prefix based on key type
        var prefix = keyType switch
        {
            ApiKeyType.Live => "ti_live_",
            ApiKeyType.Test => "ti_test_",
            ApiKeyType.ReadOnly => "ti_read_",
            _ => "ti_key_"
        };

        var rawKey = $"{prefix}{randomPart}";
        var keyHash = ComputeKeyHash(rawKey);

        return (rawKey, prefix, keyHash);
    }

    private static string ComputeKeyHash(string rawKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion
}
