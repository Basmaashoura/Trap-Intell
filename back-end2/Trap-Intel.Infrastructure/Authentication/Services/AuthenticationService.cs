using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Domain.Identity.Policies;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Models;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Information returned when 2FA is required during login.
/// </summary>
public sealed record TwoFactorLoginInfo(
    string TwoFactorToken,
    DateTime TokenExpiry,
    Guid UserId);

/// <summary>
/// Result of validating a 2FA token.
/// </summary>
public sealed record TwoFactorValidationResult(
    Guid UserId,
    string? IpAddress,
    string? UserAgent,
    bool RememberMe);

/// <summary>
/// Core authentication service handling login, registration, and token operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate a user with email and password.
    /// </summary>
    Task<Result<AuthenticationResponse>> LoginAsync(
        string email,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate user credentials without generating tokens.
    /// </summary>
    Task<Result<User>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using a valid refresh token.
    /// </summary>
    Task<Result<AuthenticationResponse>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout user and revoke their refresh token.
    /// </summary>
    Task<Result> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout user from all devices.
    /// </summary>
    Task<Result<int>> LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate authentication response for a validated user.
    /// </summary>
    Task<Result<AuthenticationResponse>> GenerateAuthenticationResponseAsync(
        User user,
        bool rememberMe = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate password strength.
    /// </summary>
    Result ValidatePasswordStrength(string password);

    /// <summary>
    /// Hash a password using BCrypt.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Get active session count for a user.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a temporary 2FA token for a user during login.
    /// </summary>
    Task<Result<TwoFactorLoginInfo>> GenerateTwoFactorLoginTokenAsync(
        User user,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a temporary 2FA login token and retrieve user information.
    /// </summary>
    Task<Result<TwoFactorValidationResult>> ValidateTwoFactorTokenAsync(
        string twoFactorToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete the login process after 2FA verification.
    /// </summary>
    Task<Result<AuthenticationResponse>> CompleteTwoFactorLoginAsync(
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRoleRepository _roleRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly LockoutSettings _lockoutSettings;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRoleRepository roleRepository,
        IOptions<JwtSettings> jwtSettings,
        IOptions<LockoutSettings> lockoutSettings,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _roleRepository = roleRepository;
        _jwtSettings = jwtSettings.Value;
        _lockoutSettings = lockoutSettings.Value;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResponse>> LoginAsync(
        string email,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        // Validate credentials
        var validationResult = await ValidateCredentialsAsync(email, password, cancellationToken);

        if (validationResult.IsFailure)
            return Result.Failure<AuthenticationResponse>(validationResult.Errors);

        var user = validationResult.Value;

        // Check if 2FA is enabled
        if (user.TwoFactorEnabled)
        {
            // Generate a temporary 2FA token and return it
            var twoFactorResult = await GenerateTwoFactorLoginTokenAsync(user, ipAddress, userAgent, rememberMe, cancellationToken);
            if (twoFactorResult.IsFailure)
                return Result.Failure<AuthenticationResponse>(twoFactorResult.Errors);

            // Return a special response indicating 2FA is required
            return Result.Failure<AuthenticationResponse>(IdentityErrors.TwoFactorRequired.WithData(new Dictionary<string, object>
            {
                ["TwoFactorToken"] = twoFactorResult.Value.TwoFactorToken,
                ["TokenExpiry"] = twoFactorResult.Value.TokenExpiry,
                ["UserId"] = twoFactorResult.Value.UserId
            }));
        }

        // Record successful login
        user.RecordSuccessfulLogin();

        // Note: Domain events will be raised through the repository/unit of work pattern
        // The UserLoggedInEvent should be raised in the application layer or through a domain service

        // Generate authentication response with refresh token
        return await GenerateAuthenticationResponseAsync(user, rememberMe, ipAddress, userAgent, cancellationToken);
    }

    public async Task<Result<User>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        // Check if account is locked
        if (user.IsLockedOut)
        {
            return Result.Failure<User>(IdentityErrors.AccountLocked);
        }

        if (!user.IsActive)
        {
            return Result.Failure<User>(IdentityErrors.AccountInactive);
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<User>(IdentityErrors.EmailNotConfirmed);
        }

        // Verify password
        var passwordResult = _passwordHashingService.VerifyPassword(user.PasswordHash, password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            // Record failed login attempt
            user.RecordFailedLogin();

            // Check if we should lock the account
            if (_lockoutSettings.EnableLockout && 
                user.GetConsecutiveFailedLogins() >= _lockoutSettings.MaxFailedAttempts)
            {
                var lockoutEnd = DateTime.UtcNow.AddMinutes(_lockoutSettings.LockoutDurationMinutes);
                user.LockAccount(lockoutEnd, "Too many failed login attempts");
            }

            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        // Check if password needs rehashing (upgraded algorithm)
        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = _passwordHashingService.HashPassword(password);
            user.SetPasswordHash(newHash);
        }

        return Result.Success(user);
    }

    public async Task<Result<AuthenticationResponse>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        // Rotate the refresh token
        var rotationResult = await _refreshTokenService.RotateTokenAsync(
            refreshToken, ipAddress, userAgent, cancellationToken);

        if (rotationResult.IsFailure)
        {
            _logger.LogWarning("Token refresh failed: {Error}", 
                rotationResult.Errors.FirstOrDefault()?.Message);
            return Result.Failure<AuthenticationResponse>(rotationResult.Errors);
        }

        var newRefreshToken = rotationResult.Value;

        // Get the user to generate new access token
        var user = await _userRepository.GetByIdAsync(newRefreshToken.Token.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User not found during token refresh: {UserId}", newRefreshToken.Token.UserId);
            return Result.Failure<AuthenticationResponse>(IdentityErrors.UserNotFound);
        }

        // Check if user is still active
        if (!user.IsActive)
        {
            _logger.LogWarning("Token refresh for inactive user: {UserId}", user.Id);
            await _refreshTokenService.RevokeAllUserTokensAsync(user.Id, "User account inactive", cancellationToken);
            return Result.Failure<AuthenticationResponse>(IdentityErrors.AccountInactive);
        }

        // Check if user is locked
        if (user.IsLockedOut)
        {
            _logger.LogWarning("Token refresh for locked user: {UserId}", user.Id);
            await _refreshTokenService.RevokeAllUserTokensAsync(user.Id, "User account locked", cancellationToken);
            return Result.Failure<AuthenticationResponse>(IdentityErrors.AccountLocked);
        }

        // Generate new access token
        var permissions = await ResolvePermissionsAsync(user, cancellationToken);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);

        var response = new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.RawToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email.Value,
                UserName = user.UserName.Value,
                FirstName = user.FirstName.Value,
                LastName = user.LastName.Value,
                FullName = user.FullName,
                RoleId = user.RoleId,
                Role = await ResolveRoleNameAsync(user.RoleId, cancellationToken),
                OrganizationId = user.OrganizationId,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Permissions = permissions
            }
        };

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
        return Result.Success(response);
    }

    public async Task<Result> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return await _refreshTokenService.RevokeTokenAsync(refreshToken, "User logout", cancellationToken);
    }

    public async Task<Result<int>> LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _refreshTokenService.RevokeAllUserTokensAsync(userId, "Logout from all devices", cancellationToken);
    }

    public async Task<Result<AuthenticationResponse>> GenerateAuthenticationResponseAsync(
        User user,
        bool rememberMe = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = await ResolvePermissionsAsync(user, cancellationToken);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);

        // Create refresh token
        var refreshTokenResult = await _refreshTokenService.CreateTokenAsync(
            user.Id, rememberMe, ipAddress, userAgent, cancellationToken);

        if (refreshTokenResult.IsFailure)
        {
            _logger.LogError("Failed to create refresh token for user: {UserId}", user.Id);
            return Result.Failure<AuthenticationResponse>(refreshTokenResult.Errors);
        }

        var refreshToken = refreshTokenResult.Value;

        return Result.Success(new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.RawToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email.Value,
                UserName = user.UserName.Value,
                FirstName = user.FirstName.Value,
                LastName = user.LastName.Value,
                FullName = user.FullName,
                RoleId = user.RoleId,
                Role = await ResolveRoleNameAsync(user.RoleId, cancellationToken),
                OrganizationId = user.OrganizationId,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Permissions = permissions
            }
        });
    }

    private async Task<string> ResolveRoleNameAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role?.Name ?? Trap_Intel.Domain.Roles.SystemRoles.GetName(roleId);
    }

    private async Task<string[]> ResolvePermissionsAsync(User user, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        if (role is not null && role.Permissions.Count > 0)
        {
            return role.Permissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return user.GetPermissions().ToArray();
    }

    public async Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _refreshTokenService.GetActiveSessionCountAsync(userId, cancellationToken);
    }

    public Result ValidatePasswordStrength(string password)
    {
        // Check against policy
        var policyResult = PasswordPolicy.ValidatePassword(password);
        if (policyResult.IsFailure)
            return policyResult;

        // Check against common passwords
        if (PasswordPolicy.IsCommonPassword(password))
        {
            return Result.Failure(Error.Custom(
                "Identity.CommonPassword",
                "This password is too common. Please choose a more secure password."));
        }

        return Result.Success();
    }

    public string HashPassword(string password)
    {
        return _passwordHashingService.HashPassword(password);
    }

    #region Two-Factor Authentication Methods

    // In-memory storage for temporary 2FA tokens (in production, use distributed cache like Redis)
    private static readonly Dictionary<string, TwoFactorPendingLogin> _pendingTwoFactorLogins = new();
    private static readonly object _lockObject = new();

    private sealed record TwoFactorPendingLogin(
        Guid UserId,
        string TokenHash,
        DateTime Expiry,
        string? IpAddress,
        string? UserAgent,
        bool RememberMe);

    public Task<Result<TwoFactorLoginInfo>> GenerateTwoFactorLoginTokenAsync(
        User user,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var rawToken = Convert.ToBase64String(tokenBytes);
        var tokenHash = HashToken(rawToken);

        // Token expires in 5 minutes
        var expiry = DateTime.UtcNow.AddMinutes(5);

        // Store pending login
        var pendingLogin = new TwoFactorPendingLogin(
            user.Id,
            tokenHash,
            expiry,
            ipAddress,
            userAgent,
            rememberMe);

        lock (_lockObject)
        {
            // Clean up expired tokens
            var expiredKeys = _pendingTwoFactorLogins
                .Where(kvp => kvp.Value.Expiry < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expiredKeys)
            {
                _pendingTwoFactorLogins.Remove(key);
            }

            _pendingTwoFactorLogins[tokenHash] = pendingLogin;
        }

        _logger.LogInformation("Generated 2FA login token for user: {UserId}", user.Id);

        return Task.FromResult(Result.Success(new TwoFactorLoginInfo(rawToken, expiry, user.Id)));
    }

    public Task<Result<TwoFactorValidationResult>> ValidateTwoFactorTokenAsync(
        string twoFactorToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(twoFactorToken);

        lock (_lockObject)
        {
            if (!_pendingTwoFactorLogins.TryGetValue(tokenHash, out var pendingLogin))
            {
                _logger.LogWarning("Invalid 2FA token attempted");
                return Task.FromResult(Result.Failure<TwoFactorValidationResult>(IdentityErrors.InvalidTwoFactorToken));
            }

            if (pendingLogin.Expiry < DateTime.UtcNow)
            {
                _pendingTwoFactorLogins.Remove(tokenHash);
                _logger.LogWarning("Expired 2FA token attempted for user: {UserId}", pendingLogin.UserId);
                return Task.FromResult(Result.Failure<TwoFactorValidationResult>(IdentityErrors.TwoFactorTokenExpired));
            }

            return Task.FromResult(Result.Success(new TwoFactorValidationResult(
                pendingLogin.UserId,
                pendingLogin.IpAddress,
                pendingLogin.UserAgent,
                pendingLogin.RememberMe)));
        }
    }

    public async Task<Result<AuthenticationResponse>> CompleteTwoFactorLoginAsync(
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User not found during 2FA completion: {UserId}", userId);
            return Result.Failure<AuthenticationResponse>(IdentityErrors.UserNotFound);
        }

        // Verify user is still active and not locked
        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResponse>(IdentityErrors.AccountInactive);
        }

        if (user.IsLockedOut)
        {
            return Result.Failure<AuthenticationResponse>(IdentityErrors.AccountLocked);
        }

        // Record successful login
        user.RecordSuccessfulLogin();

        // Generate authentication response
        var response = await GenerateAuthenticationResponseAsync(user, rememberMe, ipAddress, userAgent, cancellationToken);

        if (response.IsSuccess)
        {
            _logger.LogInformation("2FA login completed successfully for user: {UserId}", userId);

            // Clean up the pending 2FA token (find by userId)
            lock (_lockObject)
            {
                var keyToRemove = _pendingTwoFactorLogins
                    .FirstOrDefault(kvp => kvp.Value.UserId == userId)
                    .Key;
                if (keyToRemove != null)
                {
                    _pendingTwoFactorLogins.Remove(keyToRemove);
                }
            }
        }

        return response;
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

