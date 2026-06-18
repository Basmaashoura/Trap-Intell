using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Models;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for managing refresh tokens with rotation and reuse detection.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user.
    /// </summary>
    Task<Result<RefreshTokenResult>> CreateTokenAsync(
        Guid userId,
        bool rememberMe = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token (validates old token and issues new one).
    /// </summary>
    Task<Result<RefreshTokenResult>> RotateTokenAsync(
        string rawToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token.
    /// </summary>
    Task<Result<RefreshToken>> ValidateTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    Task<Result> RevokeTokenAsync(
        string rawToken,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task<Result<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active sessions for a user.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired tokens from the database.
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a refresh token operation containing the raw token and entity.
/// </summary>
public record RefreshTokenResult(
    string RawToken,
    RefreshToken Token,
    DateTime ExpiresAt);

/// <summary>
/// Implementation of refresh token service with rotation and security features.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly RefreshTokenSettings _settings;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IOptions<RefreshTokenSettings> settings,
        ILogger<RefreshTokenService> logger)
    {
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenResult>> CreateTokenAsync(
        Guid userId,
        bool rememberMe = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("Token creation attempted for non-existent user: {UserId}", userId);
                return Result.Failure<RefreshTokenResult>(IdentityErrors.UserNotFound);
            }

            // Check session limit
            var activeSessions = await _refreshTokenRepository.CountActiveSessionsAsync(userId, cancellationToken);
            if (activeSessions >= _settings.MaxActiveSessions)
            {
                if (_settings.RevokeOldestOnMaxExceeded)
                {
                    // Revoke oldest session
                    var activeTokens = await _refreshTokenRepository.GetActiveByUserAsync(userId, cancellationToken);
                    var oldestToken = activeTokens.OrderBy(t => t.CreatedAt).FirstOrDefault();
                    if (oldestToken != null)
                    {
                        oldestToken.Revoke("Session limit exceeded - oldest session revoked");
                        await _refreshTokenRepository.UpdateAsync(oldestToken, cancellationToken);
                        _logger.LogInformation("Revoked oldest session for user {UserId} due to session limit", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("User {UserId} has exceeded max sessions ({Max})", userId, _settings.MaxActiveSessions);
                    return Result.Failure<RefreshTokenResult>(
                        new Error("RefreshToken.MaxSessionsExceeded", 
                            $"Maximum number of active sessions ({_settings.MaxActiveSessions}) exceeded"));
                }
            }

            // Generate new token
            var rawToken = RefreshToken.GenerateSecureToken();
            var lifetime = _settings.GetTokenLifetime(rememberMe);
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            var refreshToken = RefreshToken.Create(
                userId,
                rawToken,
                expiresAt,
                ipAddress,
                userAgent);

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            _logger.LogInformation(
                "Created refresh token for user {UserId}, expires at {ExpiresAt}, RememberMe: {RememberMe}",
                userId, expiresAt, rememberMe);

            return Result.Success(new RefreshTokenResult(rawToken, refreshToken, expiresAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for user {UserId}", userId);
            return Result.Failure<RefreshTokenResult>(
                new Error("RefreshToken.CreateFailed", "Failed to create refresh token"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenResult>> RotateTokenAsync(
        string rawToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the existing token
            var validationResult = await ValidateTokenAsync(rawToken, cancellationToken);
            if (validationResult.IsFailure)
            {
            return Result.Failure<RefreshTokenResult>(validationResult.Errors.First());
            }

            var existingToken = validationResult.Value;

            // Check for token reuse (security threat)
            if (existingToken.IsUsed)
            {
                if (_settings.EnableReuseDetection)
                {
                    _logger.LogWarning(
                        "Token reuse detected! Token: {TokenId}, User: {UserId}, Family: {FamilyId}. Revoking entire family.",
                        existingToken.Id, existingToken.UserId, existingToken.FamilyId);

                    // Revoke entire token family - potential security breach
                    await _refreshTokenRepository.RevokeAllInFamilyAsync(
                        existingToken.FamilyId,
                        "Token reuse detected - potential security breach",
                        cancellationToken);

                    return Result.Failure<RefreshTokenResult>(
                        new Error("RefreshToken.ReuseDetected", 
                            "Security alert: Token reuse detected. All sessions have been terminated for security."));
                }
            }

            // Check for grace period (concurrent requests)
            if (existingToken.UsedAt.HasValue)
            {
                var timeSinceUse = DateTime.UtcNow - existingToken.UsedAt.Value;
                if (timeSinceUse.TotalSeconds <= _settings.ConcurrentUseGracePeriodSeconds)
                {
                    _logger.LogDebug(
                        "Token {TokenId} within grace period, allowing concurrent use",
                        existingToken.Id);
                    
                    // Return the replacement token info if available
                    if (existingToken.ReplacedByTokenId.HasValue)
                    {
                        var replacementToken = await _refreshTokenRepository.GetByIdAsync(
                            existingToken.ReplacedByTokenId.Value, cancellationToken);
                        
                        if (replacementToken != null && replacementToken.IsValid)
                        {
                            // Note: We can't return the raw token since we only stored the hash
                            // The client should have received it in the original rotation response
                            return Result.Failure<RefreshTokenResult>(
                                new Error("RefreshToken.AlreadyRotated", 
                                    "Token has already been rotated. Use the new token from the previous response."));
                        }
                    }
                }
            }

            // Generate new token in the same family
            var newRawToken = RefreshToken.GenerateSecureToken();
            var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
            
            // Determine if this was a "remember me" session based on original token lifetime
            var originalLifetime = existingToken.ExpiresAt - existingToken.CreatedAt;
            var isRememberMe = originalLifetime.TotalDays > _settings.TokenLifetimeDays;
            var lifetime = _settings.GetTokenLifetime(isRememberMe);
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            var newToken = RefreshToken.CreateRotated(
                existingToken,
                newRawToken,
                expiresAt,
                ipAddress,
                userAgent);

            // Mark old token as used
            existingToken.MarkAsUsed(newToken.Id);

            // Save changes
            await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);
            await _refreshTokenRepository.AddAsync(newToken, cancellationToken);

            _logger.LogInformation(
                "Rotated refresh token for user {UserId}. Old: {OldTokenId}, New: {NewTokenId}",
                existingToken.UserId, existingToken.Id, newToken.Id);

            return Result.Success(new RefreshTokenResult(newRawToken, newToken, expiresAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating refresh token");
            return Result.Failure<RefreshTokenResult>(
                new Error("RefreshToken.RotationFailed", "Failed to rotate refresh token"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<RefreshToken>> ValidateTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Result.Failure<RefreshToken>(
                new Error("RefreshToken.Empty", "Refresh token is required"));
        }

        var tokenHash = RefreshToken.HashToken(rawToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            _logger.LogWarning("Refresh token not found");
            return Result.Failure<RefreshToken>(
                new Error("RefreshToken.NotFound", "Invalid refresh token"));
        }

        if (token.IsRevoked)
        {
            _logger.LogWarning(
                "Attempted to use revoked token: {TokenId}, Revoked at: {RevokedAt}, Reason: {Reason}",
                token.Id, token.RevokedAt, token.RevocationReason);
            return Result.Failure<RefreshToken>(
                new Error("RefreshToken.Revoked", "Refresh token has been revoked"));
        }

        if (token.IsExpired)
        {
            _logger.LogInformation("Refresh token expired: {TokenId}", token.Id);
            return Result.Failure<RefreshToken>(
                new Error("RefreshToken.Expired", "Refresh token has expired"));
        }

        // Note: We check IsUsed in RotateTokenAsync for reuse detection logic
        // Here we just return the token for the caller to handle

        return Result.Success(token);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(
        string rawToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return Result.Failure(new Error("RefreshToken.Empty", "Refresh token is required"));
            }

            var tokenHash = RefreshToken.HashToken(rawToken);
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null)
            {
                _logger.LogWarning("Attempted to revoke non-existent token");
                return Result.Failure(new Error("RefreshToken.NotFound", "Token not found"));
            }

            if (token.IsRevoked)
            {
                _logger.LogDebug("Token {TokenId} is already revoked", token.Id);
                return Result.Success(); // Idempotent operation
            }

            token.Revoke(reason);
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

            _logger.LogInformation(
                "Revoked refresh token {TokenId} for user {UserId}. Reason: {Reason}",
                token.Id, token.UserId, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            return Result.Failure(new Error("RefreshToken.RevokeFailed", "Failed to revoke token"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var revokedCount = await _refreshTokenRepository.RevokeAllForUserAsync(
                userId, reason, cancellationToken);

            _logger.LogInformation(
                "Revoked {Count} refresh tokens for user {UserId}. Reason: {Reason}",
                revokedCount, userId, reason);

            return Result.Success(revokedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            return Result.Failure<int>(
                new Error("RefreshToken.RevokeAllFailed", "Failed to revoke all tokens"));
        }
    }

    /// <inheritdoc />
    public async Task<int> GetActiveSessionCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _refreshTokenRepository.CountActiveSessionsAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _refreshTokenRepository.DeleteExpiredAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired refresh tokens", deletedCount);
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired tokens");
            return 0;
        }
    }
}
