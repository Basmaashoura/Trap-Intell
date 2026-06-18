using SharedKernel;

namespace Application.Abstractions.Identity;

/// <summary>
/// Service for authentication and identity management.
/// Abstracts ASP.NET Identity from the Application layer.
/// </summary>
public interface IIdentityService
{
    #region User Management

    /// <summary>
    /// Creates a new identity user linked to a domain user.
    /// </summary>
    Task<Result<Guid>> CreateUserAsync(
        Guid domainUserId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user email in identity.
    /// </summary>
    Task<Result> UpdateEmailAsync(
        Guid domainUserId,
        string newEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an identity user.
    /// </summary>
    Task<Result> DeleteUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an identity user exists for a domain user.
    /// </summary>
    Task<bool> UserExistsAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Authentication

    /// <summary>
    /// Authenticates user and returns tokens.
    /// </summary>
    Task<Result<TokenResponse>> LoginAsync(
        string email,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out user and revokes tokens.
    /// </summary>
    Task<Result> LogoutAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<Result<TokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user (force logout).
    /// </summary>
    Task<Result> RevokeAllTokensAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token by its hash.
    /// </summary>
    Task<Result> RevokeRefreshTokenAsync(
        Guid domainUserId,
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    #endregion

    #region Password Management

    /// <summary>
    /// Changes user password.
    /// </summary>
    Task<Result> ChangePasswordAsync(
        Guid domainUserId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates password reset token.
    /// </summary>
    Task<Result<string>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets password using token.
    /// </summary>
    Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    #endregion

    #region Two-Factor Authentication

    /// <summary>
    /// Enables 2FA and returns setup info.
    /// </summary>
    Task<Result<TwoFactorSetupInfo>> EnableTwoFactorAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies and confirms 2FA setup.
    /// </summary>
    Task<Result> ConfirmTwoFactorAsync(
        Guid domainUserId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA.
    /// </summary>
    Task<Result> DisableTwoFactorAsync(
        Guid domainUserId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies 2FA code during login.
    /// </summary>
    Task<Result<TokenResponse>> VerifyTwoFactorAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has 2FA enabled.
    /// </summary>
    Task<bool> IsTwoFactorEnabledAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Account Status

    /// <summary>
    /// Deactivates user identity (offboarding).
    /// </summary>
    Task<Result> DeactivateUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates user identity.
    /// </summary>
    Task<Result> ActivateUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user is locked out.
    /// </summary>
    Task<bool> IsLockedOutAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Email Confirmation

    /// <summary>
    /// Generates email confirmation token.
    /// </summary>
    Task<Result<string>> GenerateEmailConfirmationTokenAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms email with token.
    /// </summary>
    Task<Result> ConfirmEmailAsync(
        Guid domainUserId,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email is confirmed.
    /// </summary>
    Task<bool> IsEmailConfirmedAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Role Management

    /// <summary>
    /// Creates an identity role linked to a domain role.
    /// </summary>
    Task<Result> CreateRoleAsync(
        Guid domainRoleId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an identity role.
    /// </summary>
    Task<Result> DeleteRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns role to user.
    /// </summary>
    Task<Result> AssignRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes role from user.
    /// </summary>
    Task<Result> RemoveRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's roles.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user is in role.
    /// </summary>
    Task<bool> IsInRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default);

    #endregion
}
