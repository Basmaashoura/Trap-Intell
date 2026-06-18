using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.ApiKeys;

public static class ApiKeyErrors
{
    #region Validation Errors

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "ApiKey.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidUserId = Error.Custom(
        "ApiKey.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidName = Error.Custom(
        "ApiKey.InvalidName",
        "API key name cannot be empty");

    public static readonly Error NameTooLong = Error.Custom(
        "ApiKey.NameTooLong",
        "API key name cannot exceed 100 characters");

    public static readonly Error InvalidReason = Error.Custom(
        "ApiKey.InvalidReason",
        "Reason cannot be empty");

    public static readonly Error InvalidRevocationReason = Error.Custom(
        "ApiKey.InvalidRevocationReason",
        "Revocation reason cannot be empty");

    public static readonly Error ExpirationInPast = Error.Custom(
        "ApiKey.ExpirationInPast",
        "Expiration date must be in the future");

    public static readonly Error InvalidRateLimit = Error.Custom(
        "ApiKey.InvalidRateLimit",
        "Rate limit configuration is invalid");

    public static readonly Error NoPermissionsSpecified = Error.Custom(
        "ApiKey.NoPermissionsSpecified",
        "At least one permission must be specified");

    #endregion

    #region Status Errors

    public static readonly Error KeyNotActive = Error.Custom(
        "ApiKey.KeyNotActive",
        "API key is not active");

    public static readonly Error KeyExpired = Error.Custom(
        "ApiKey.KeyExpired",
        "API key has expired");

    public static readonly Error AlreadyRevoked = Error.Custom(
        "ApiKey.AlreadyRevoked",
        "API key is already revoked");

    public static readonly Error CannotRotateInactiveKey = Error.Custom(
        "ApiKey.CannotRotateInactiveKey",
        "Cannot rotate an inactive API key");

    public static readonly Error CannotSuspendRevokedKey = Error.Custom(
        "ApiKey.CannotSuspendRevokedKey",
        "Cannot suspend a revoked API key");

    public static readonly Error CannotReactivateNonSuspendedKey = Error.Custom(
        "ApiKey.CannotReactivateNonSuspendedKey",
        "Can only reactivate suspended API keys");

    public static readonly Error CannotReactivateExpiredKey = Error.Custom(
        "ApiKey.CannotReactivateExpiredKey",
        "Cannot reactivate an expired API key");

    public static readonly Error CannotUpdateRevokedKey = Error.Custom(
        "ApiKey.CannotUpdateRevokedKey",
        "Cannot update a revoked API key");

    #endregion

    #region Access Errors

    public static readonly Error IPNotAllowed = Error.Custom(
        "ApiKey.IPNotAllowed",
        "Request IP address is not in the whitelist");

    public static readonly Error RateLimitExceeded = Error.Custom(
        "ApiKey.RateLimitExceeded",
        "Rate limit exceeded. Please try again later");

    public static readonly Error InsufficientPermissions = Error.Custom(
        "ApiKey.InsufficientPermissions",
        "API key does not have required permissions");

    public static readonly Error InvalidKeyFormat = Error.Custom(
        "ApiKey.InvalidKeyFormat",
        "Invalid API key format");

    public static readonly Error KeyNotFound = Error.Custom(
        "ApiKey.KeyNotFound",
        "API key not found");

    public static readonly Error KeyValidationFailed = Error.Custom(
        "ApiKey.KeyValidationFailed",
        "API key validation failed");

    #endregion

    #region Quota Errors

    public static readonly Error MaxKeysReached = Error.Custom(
        "ApiKey.MaxKeysReached",
        "Maximum number of API keys reached for this organization");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid apiKeyId) => Error.Custom(
        "ApiKey.NotFound",
        $"API key with ID '{apiKeyId}' not found");

    public static Error NotFoundByPrefix(string keyPrefix) => Error.Custom(
        "ApiKey.NotFound",
        $"API key with prefix '{keyPrefix}' not found");

    public static Error PermissionDenied(string permission) => Error.Custom(
        "ApiKey.PermissionDenied",
        $"API key does not have '{permission}' permission");

    public static Error RateLimitExceededWithDetails(int limit, string scope) => Error.Custom(
        "ApiKey.RateLimitExceeded",
        $"Rate limit of {limit} requests per {scope} exceeded");

    #endregion
}
