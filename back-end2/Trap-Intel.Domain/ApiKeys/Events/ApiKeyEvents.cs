using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.ApiKeys.Enums;

namespace Trap_Intel.Domain.ApiKeys.Events;

/// <summary>
/// API key created.
/// </summary>
public record ApiKeyCreatedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string Name,
    ApiKeyType KeyType,
    string KeyPrefix,
    Guid CreatedByUserId,
    DateTime? ExpiresAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key used successfully.
/// </summary>
public record ApiKeyUsedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string IPAddress,
    string Endpoint,
    long TotalUsageCount,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key usage failed.
/// </summary>
public record ApiKeyUsageFailedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string IPAddress,
    string Endpoint,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key revoked.
/// </summary>
public record ApiKeyRevokedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    Guid RevokedByUserId,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key rotated (new key generated).
/// </summary>
public record ApiKeyRotatedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string OldKeyPrefix,
    string NewKeyPrefix,
    int NewVersion,
    Guid RotatedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key status changed.
/// </summary>
public record ApiKeyStatusChangedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    ApiKeyStatus OldStatus,
    ApiKeyStatus NewStatus,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key permissions updated.
/// </summary>
public record ApiKeyPermissionsUpdatedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    List<ApiKeyPermission> OldPermissions,
    List<ApiKeyPermission> NewPermissions,
    Guid UpdatedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key rate limit updated.
/// </summary>
public record ApiKeyRateLimitUpdatedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    int NewRequestsPerMinute,
    int NewRequestsPerHour,
    int NewRequestsPerDay,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key expiration extended.
/// </summary>
public record ApiKeyExpirationExtendedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    DateTime NewExpiresAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key expired (detected by system).
/// </summary>
public record ApiKeyExpiredEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    DateTime ExpiredAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key rate limit exceeded.
/// </summary>
public record ApiKeyRateLimitExceededEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string IPAddress,
    int CurrentUsage,
    int Limit,
    string Scope,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key expiring soon warning.
/// </summary>
public record ApiKeyExpiringSoonEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    DateTime ExpiresAt,
    int DaysRemaining,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key IP whitelist updated.
/// </summary>
public record ApiKeyIPWhitelistUpdatedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    List<string> AllowedIPs,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// API key access denied (IP not in whitelist).
/// </summary>
public record ApiKeyIPDeniedEvent(
    Guid ApiKeyId,
    Guid OrganizationId,
    string DeniedIP,
    DateTime OccurredOn) : IDomainEvent;
