using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Identity;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for managing email verification and password reset tokens.
/// </summary>
public sealed class EmailTokenService : IEmailTokenService
{
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmailVerificationSettings _emailVerificationSettings;
    private readonly PasswordResetSettings _passwordResetSettings;
    private readonly ILogger<EmailTokenService> _logger;

    public EmailTokenService(
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IOptions<EmailVerificationSettings> emailVerificationSettings,
        IOptions<PasswordResetSettings> passwordResetSettings,
        ILogger<EmailTokenService> logger)
    {
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _emailVerificationSettings = emailVerificationSettings.Value;
        _passwordResetSettings = passwordResetSettings.Value;
        _logger = logger;
    }

    #region Email Verification

    /// <summary>
    /// Creates and sends an email verification token.
    /// </summary>
    public async Task<Result<Guid>> CreateEmailVerificationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<Guid>(IdentityErrors.UserNotFound);

        if (user.EmailConfirmed)
            return Result.Failure<Guid>(IdentityErrors.EmailAlreadyConfirmed);

        // Revoke any existing tokens
        await _emailVerificationTokenRepository.RevokeAllForUserAsync(userId, cancellationToken);

        // Create new token
        var (token, rawToken) = EmailVerificationToken.Create(
            userId,
            _emailVerificationSettings.TokenExpirationHours);

        await _emailVerificationTokenRepository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email
        await _emailService.SendEmailVerificationAsync(
            user.Email.Value,
            user.FullName,
            rawToken,
            cancellationToken);

        _logger.LogInformation(
            "Email verification token created for user {UserId}, expires at {ExpiresAt}",
            userId, token.ExpiresAt);

        return Result.Success(token.Id);
    }

    /// <summary>
    /// Verifies an email using the provided token.
    /// </summary>
    public async Task<Result> VerifyEmailAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = EmailVerificationToken.HashToken(rawToken);
        var token = await _emailVerificationTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            _logger.LogWarning("Email verification attempt with invalid token hash");
            return Result.Failure(IdentityErrors.EmailVerificationTokenNotFound);
        }

        if (!token.IsValid)
        {
            _logger.LogWarning(
                "Email verification attempt with invalid token state: Used={IsUsed}, Revoked={IsRevoked}, Expired={IsExpired}",
                token.IsUsed, token.IsRevoked, token.IsExpired);

            if (token.IsUsed)
                return Result.Failure(IdentityErrors.EmailVerificationTokenAlreadyUsed);
            if (token.IsRevoked)
                return Result.Failure(IdentityErrors.EmailVerificationTokenRevoked);
            if (token.IsExpired)
                return Result.Failure(IdentityErrors.EmailVerificationTokenExpired);

            return Result.Failure(IdentityErrors.InvalidEmailVerificationToken);
        }

        // Validate the raw token
        if (!token.ValidateToken(rawToken))
        {
            _logger.LogWarning("Email verification token validation failed");
            return Result.Failure(IdentityErrors.InvalidEmailVerificationToken);
        }

        // Mark token as used
        var useResult = token.Use();
        if (useResult.IsFailure)
            return useResult;

        // Confirm user's email
        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        var confirmResult = user.ConfirmEmail();
        if (confirmResult.IsFailure)
            return confirmResult;

        _emailVerificationTokenRepository.Update(token);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(
            user.Email.Value,
            user.FullName,
            cancellationToken);

        _logger.LogInformation("Email verified successfully for user {UserId}", token.UserId);

        return Result.Success();
    }

    /// <summary>
    /// Resends the email verification token.
    /// </summary>
    public async Task<Result> ResendEmailVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Don't reveal if user exists
            _logger.LogInformation("Email verification resend request for non-existent email");
            return Result.Success();
        }

        if (user.EmailConfirmed)
        {
            // Don't reveal if already confirmed
            _logger.LogInformation("Email verification resend request for already confirmed user {UserId}", user.Id);
            return Result.Success();
        }

        await CreateEmailVerificationTokenAsync(user.Id, cancellationToken);
        return Result.Success();
    }

    #endregion

    #region Password Reset

    /// <summary>
    /// Creates and sends a password reset token.
    /// </summary>
    public async Task<Result> RequestPasswordResetAsync(
        string email,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Don't reveal if user exists - always return success
            _logger.LogInformation("Password reset request for non-existent email");
            return Result.Success();
        }

        // Rate limiting check
        var recentCount = await _passwordResetTokenRepository.GetRecentTokenCountAsync(
            user.Id,
            TimeSpan.FromMinutes(_passwordResetSettings.RateLimitWindowMinutes),
            cancellationToken);

        if (recentCount >= _passwordResetSettings.MaxRequestsPerWindow)
        {
            _logger.LogWarning(
                "Password reset rate limit exceeded for user {UserId}. Count: {Count}",
                user.Id, recentCount);
            // Don't reveal rate limiting - still return success
            return Result.Success();
        }

        // Revoke any existing tokens
        await _passwordResetTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        // Create new token
        var (token, rawToken) = PasswordResetToken.Create(
            user.Id,
            ipAddress,
            userAgent,
            _passwordResetSettings.TokenExpirationMinutes);

        await _passwordResetTokenRepository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email
        await _emailService.SendPasswordResetAsync(
            user.Email.Value,
            user.FullName,
            rawToken,
            cancellationToken);

        _logger.LogInformation(
            "Password reset token created for user {UserId}, expires at {ExpiresAt}",
            user.Id, token.ExpiresAt);

        return Result.Success();
    }

    /// <summary>
    /// Validates a password reset token without using it.
    /// </summary>
    public async Task<Result<Guid>> ValidatePasswordResetTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = PasswordResetToken.HashToken(rawToken);
        var token = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || !token.IsValid || !token.ValidateToken(rawToken))
        {
            return Result.Failure<Guid>(IdentityErrors.InvalidPasswordResetToken);
        }

        return Result.Success(token.UserId);
    }

    /// <summary>
    /// Resets the password using the provided token.
    /// </summary>
    public async Task<Result> ResetPasswordAsync(
        string rawToken,
        string newPassword,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = PasswordResetToken.HashToken(rawToken);
        var token = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            _logger.LogWarning("Password reset attempt with invalid token hash");
            return Result.Failure(IdentityErrors.PasswordResetTokenNotFound);
        }

        if (!token.IsValid)
        {
            _logger.LogWarning(
                "Password reset attempt with invalid token state: Used={IsUsed}, Revoked={IsRevoked}, Expired={IsExpired}",
                token.IsUsed, token.IsRevoked, token.IsExpired);

            if (token.IsUsed)
                return Result.Failure(IdentityErrors.PasswordResetTokenAlreadyUsed);
            if (token.IsRevoked)
                return Result.Failure(IdentityErrors.PasswordResetTokenRevoked);
            if (token.IsExpired)
                return Result.Failure(IdentityErrors.PasswordResetTokenExpired);

            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);
        }

        // Validate the raw token
        if (!token.ValidateToken(rawToken))
        {
            _logger.LogWarning("Password reset token validation failed");
            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);
        }

        // Update user's password
        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
        if (appUser is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        var identityResetToken = await _userManager.GeneratePasswordResetTokenAsync(appUser);
        var identityResetResult = await _userManager.ResetPasswordAsync(appUser, identityResetToken, newPassword);

        if (!identityResetResult.Succeeded)
        {
            var error = identityResetResult.Errors.FirstOrDefault()?.Description ?? "Password reset failed";
            return Result.Failure(Error.Custom("Identity.PasswordResetFailed", error));
        }

        if (string.IsNullOrWhiteSpace(appUser.PasswordHash))
        {
            return Result.Failure(Error.Custom("Identity.PasswordResetFailed", "Identity did not persist a password hash."));
        }

        // Mark token as used only after password update succeeds.
        var useResult = token.Use(ipAddress);
        if (useResult.IsFailure)
            return useResult;

        var setPasswordHashResult = user.SetPasswordHash(appUser.PasswordHash);
        if (setPasswordHashResult.IsFailure)
            return setPasswordHashResult;

        _passwordResetTokenRepository.Update(token);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send password changed notification
        await _emailService.SendPasswordChangedNotificationAsync(
            user.Email.Value,
            user.FullName,
            ipAddress,
            cancellationToken);

        _logger.LogInformation("Password reset successfully for user {UserId}", token.UserId);

        return Result.Success();
    }

    #endregion
}
