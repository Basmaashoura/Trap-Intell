using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Interface for email token management service.
/// Handles email verification and password reset token operations.
/// </summary>
public interface IEmailTokenService
{
    #region Email Verification

    /// <summary>
    /// Creates and sends an email verification token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the token ID if successful.</returns>
    Task<Result<Guid>> CreateEmailVerificationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an email using the provided token.
    /// </summary>
    /// <param name="rawToken">The raw token received from the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> VerifyEmailAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends the email verification token.
    /// Always returns success to prevent email enumeration.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result (always success for security).</returns>
    Task<Result> ResendEmailVerificationAsync(
        string email,
        CancellationToken cancellationToken = default);

    #endregion

    #region Password Reset

    /// <summary>
    /// Creates and sends a password reset token.
    /// Always returns success to prevent email enumeration.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="ipAddress">IP address of the requester.</param>
    /// <param name="userAgent">User agent of the requester.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result (always success for security).</returns>
    Task<Result> RequestPasswordResetAsync(
        string email,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password reset token without using it.
    /// </summary>
    /// <param name="rawToken">The raw token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the user ID if valid.</returns>
    Task<Result<Guid>> ValidatePasswordResetTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the password using the provided token.
    /// </summary>
    /// <param name="rawToken">The raw token received from the user.</param>
    /// <param name="newPassword">The new plain-text password to set.</param>
    /// <param name="ipAddress">IP address where the reset was performed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResetPasswordAsync(
        string rawToken,
        string newPassword,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    #endregion
}
