using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Interface for Two-Factor Authentication management service.
/// Handles 2FA setup, verification, and backup code operations.
/// </summary>
public interface ITwoFactorAuthService
{
    /// <summary>
    /// Initiates 2FA setup for a user. Returns setup info including QR code.
    /// </summary>
    Task<Result<TwoFactorSetupResult>> InitiateSetupAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms 2FA setup by verifying the first TOTP code.
    /// Enables 2FA and generates backup codes.
    /// </summary>
    Task<Result<TwoFactorConfirmResult>> ConfirmSetupAsync(
        Guid userId,
        string setupToken,
        string totpCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a TOTP code for an authenticated user.
    /// </summary>
    Task<Result> VerifyTotpCodeAsync(
        Guid userId,
        string code,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a backup code for an authenticated user.
    /// </summary>
    Task<Result<int>> VerifyBackupCodeAsync(
        Guid userId,
        string code,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA for a user.
    /// </summary>
    Task<Result> DisableTwoFactorAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates backup codes (invalidates old codes).
    /// </summary>
    Task<Result<IReadOnlyList<string>>> RegenerateBackupCodesAsync(
        Guid userId,
        string totpCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of remaining backup codes for a user.
    /// </summary>
    Task<int> GetRemainingBackupCodeCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if 2FA is enabled for a user.
    /// </summary>
    Task<bool> IsTwoFactorEnabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of initiating 2FA setup.
/// </summary>
public sealed record TwoFactorSetupResult
{
    /// <summary>
    /// Temporary token for confirming setup.
    /// Valid for limited time.
    /// </summary>
    public required string SetupToken { get; init; }

    /// <summary>
    /// The TOTP secret (Base32 encoded).
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// Secret formatted for manual entry.
    /// </summary>
    public required string SecretFormatted { get; init; }

    /// <summary>
    /// QR code as Base64 PNG or OTP Auth URI for frontend QR generation.
    /// </summary>
    public required string QrCodeData { get; init; }

    /// <summary>
    /// The OTP Auth URI.
    /// </summary>
    public required string OtpAuthUri { get; init; }

    /// <summary>
    /// When the setup token expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Result of confirming 2FA setup.
/// </summary>
public sealed record TwoFactorConfirmResult
{
    /// <summary>
    /// Whether 2FA was successfully enabled.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// List of backup codes (only returned once, user must save these).
    /// </summary>
    public required IReadOnlyList<string> BackupCodes { get; init; }

    /// <summary>
    /// Message for the user.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Service for managing Two-Factor Authentication.
/// </summary>
public sealed class TwoFactorAuthService : ITwoFactorAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TwoFactorSettings _settings;
    private readonly ILogger<TwoFactorAuthService> _logger;

    // In-memory cache for setup tokens (in production, use distributed cache like Redis)
    private static readonly Dictionary<string, TwoFactorSetupData> _setupTokens = new();
    private static readonly object _lockObject = new();
    private const int SetupTokenExpirationMinutes = 10;

    public TwoFactorAuthService(
        IUserRepository userRepository,
        ITwoFactorBackupCodeRepository backupCodeRepository,
        ITwoFactorService twoFactorService,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IOptions<TwoFactorSettings> settings,
        ILogger<TwoFactorAuthService> logger)
    {
        _userRepository = userRepository;
        _backupCodeRepository = backupCodeRepository;
        _twoFactorService = twoFactorService;
        _passwordHashingService = passwordHashingService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initiates 2FA setup for a user.
    /// </summary>
    public async Task<Result<TwoFactorSetupResult>> InitiateSetupAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<TwoFactorSetupResult>(IdentityErrors.UserNotFound);

        if (user.TwoFactorEnabled)
            return Result.Failure<TwoFactorSetupResult>(IdentityErrors.TwoFactorAlreadyEnabled);

        // Generate setup info
        var setupInfo = _twoFactorService.GenerateSetupInfo(user.Email.Value, user.FullName);

        // Create setup token
        var setupToken = GenerateSetupToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(SetupTokenExpirationMinutes);

        // Store temporarily (in production, use distributed cache)
        lock (_lockObject)
        {
            // Clean up expired tokens
            CleanupExpiredSetupTokens();

            _setupTokens[setupToken] = new TwoFactorSetupData
            {
                UserId = userId,
                Secret = setupInfo.Secret,
                ExpiresAt = expiresAt
            };
        }

        _logger.LogInformation("2FA setup initiated for user {UserId}", userId);

        return Result.Success(new TwoFactorSetupResult
        {
            SetupToken = setupToken,
            Secret = setupInfo.Secret,
            SecretFormatted = setupInfo.SecretFormatted,
            QrCodeData = setupInfo.QrCodeBase64,
            OtpAuthUri = setupInfo.OtpAuthUri,
            ExpiresAt = expiresAt
        });
    }

    /// <summary>
    /// Confirms 2FA setup by verifying the first TOTP code.
    /// </summary>
    public async Task<Result<TwoFactorConfirmResult>> ConfirmSetupAsync(
        Guid userId,
        string setupToken,
        string totpCode,
        CancellationToken cancellationToken = default)
    {
        // Validate setup token
        TwoFactorSetupData? setupData;
        lock (_lockObject)
        {
            if (!_setupTokens.TryGetValue(setupToken, out setupData))
                return Result.Failure<TwoFactorConfirmResult>(IdentityErrors.InvalidTwoFactorSetupToken);

            if (setupData.UserId != userId || setupData.ExpiresAt < DateTime.UtcNow)
            {
                _setupTokens.Remove(setupToken);
                return Result.Failure<TwoFactorConfirmResult>(IdentityErrors.InvalidTwoFactorSetupToken);
            }
        }

        // Verify TOTP code
        if (!_twoFactorService.ValidateTotpCode(setupData.Secret, totpCode))
        {
            _logger.LogWarning("2FA setup confirmation failed - invalid TOTP code for user {UserId}", userId);
            return Result.Failure<TwoFactorConfirmResult>(IdentityErrors.InvalidTwoFactorCode);
        }

        // Get user and enable 2FA
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<TwoFactorConfirmResult>(IdentityErrors.UserNotFound);

        var enableResult = user.EnableTwoFactor(setupData.Secret);
        if (enableResult.IsFailure)
            return Result.Failure<TwoFactorConfirmResult>(enableResult.Errors.First());

        // Generate and save backup codes
        var backupCodePairs = TwoFactorBackupCode.CreateMultiple(userId, _settings.BackupCodeCount);
        var rawCodes = new List<string>(backupCodePairs.Count);
        
        foreach (var (code, rawCode) in backupCodePairs)
        {
            await _backupCodeRepository.AddAsync(code, cancellationToken);
            rawCodes.Add(rawCode);
        }

        // Save changes
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Remove setup token
        lock (_lockObject)
        {
            _setupTokens.Remove(setupToken);
        }

        // Send notification
        if (_settings.SendNotificationOnStatusChange)
        {
            await _emailService.SendSecurityAlertAsync(
                user.Email.Value,
                user.FullName,
                "Two-Factor Authentication Enabled",
                "Two-factor authentication has been enabled on your account.",
                cancellationToken);
        }

        _logger.LogInformation("2FA enabled successfully for user {UserId}", userId);

        return Result.Success(new TwoFactorConfirmResult
        {
            Success = true,
            BackupCodes = rawCodes.AsReadOnly(),
            Message = "Two-factor authentication has been enabled. Please save your backup codes securely."
        });
    }

    /// <summary>
    /// Verifies a TOTP code.
    /// </summary>
    public async Task<Result> VerifyTotpCodeAsync(
        Guid userId,
        string code,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result.Failure(IdentityErrors.TwoFactorNotEnabled);

        if (_twoFactorService.ValidateTotpCode(user.TwoFactorSecret, code))
        {
            _logger.LogInformation("2FA TOTP verification succeeded for user {UserId}", userId);
            return Result.Success();
        }

        _logger.LogWarning("2FA TOTP verification failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
        return Result.Failure(IdentityErrors.InvalidTwoFactorCode);
    }

    /// <summary>
    /// Verifies a backup code.
    /// </summary>
    public async Task<Result<int>> VerifyBackupCodeAsync(
        Guid userId,
        string code,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<int>(IdentityErrors.UserNotFound);

        if (!user.TwoFactorEnabled)
            return Result.Failure<int>(IdentityErrors.TwoFactorNotEnabled);

        // Get unused codes for user
        var unusedCodes = await _backupCodeRepository.GetUnusedCodesForUserAsync(userId, cancellationToken);
        
        if (unusedCodes.Count == 0)
            return Result.Failure<int>(IdentityErrors.NoBackupCodesRemaining);

        // Find matching code
        var normalizedCode = code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var codeHash = TwoFactorBackupCode.HashCode(normalizedCode);

        var matchingCode = unusedCodes.FirstOrDefault(c => c.CodeHash == codeHash);
        if (matchingCode is null)
        {
            _logger.LogWarning("2FA backup code verification failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return Result.Failure<int>(IdentityErrors.InvalidBackupCode);
        }

        // Mark code as used
        var useResult = matchingCode.Use(ipAddress);
        if (useResult.IsFailure)
            return Result.Failure<int>(useResult.Errors.First());

        _backupCodeRepository.Update(matchingCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var remainingCodes = unusedCodes.Count - 1;

        // Alert if running low on codes
        if (_settings.AlertOnLowBackupCodes && remainingCodes <= _settings.LowBackupCodesThreshold)
        {
            await _emailService.SendSecurityAlertAsync(
                user.Email.Value,
                user.FullName,
                "Low Backup Codes",
                $"You only have {remainingCodes} backup codes remaining. Consider generating new backup codes.",
                cancellationToken);
        }

        _logger.LogInformation(
            "2FA backup code used for user {UserId}. Remaining codes: {RemainingCodes}",
            userId, remainingCodes);

        return Result.Success(remainingCodes);
    }

    /// <summary>
    /// Disables 2FA for a user.
    /// </summary>
    public async Task<Result> DisableTwoFactorAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        if (!user.TwoFactorEnabled)
            return Result.Failure(IdentityErrors.TwoFactorNotEnabled);

        // Verify password
        var passwordResult = _passwordHashingService.VerifyPassword(user.PasswordHash, password);
        if (passwordResult == PasswordVerificationResult.Failed)
            return Result.Failure(IdentityErrors.InvalidCredentials);

        // Disable 2FA
        var disableResult = user.DisableTwoFactor();
        if (disableResult.IsFailure)
            return disableResult;

        // Delete all backup codes
        await _backupCodeRepository.DeleteAllForUserAsync(userId, cancellationToken);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        if (_settings.SendNotificationOnStatusChange)
        {
            await _emailService.SendSecurityAlertAsync(
                user.Email.Value,
                user.FullName,
                "Two-Factor Authentication Disabled",
                "Two-factor authentication has been disabled on your account. If you did not make this change, please secure your account immediately.",
                cancellationToken);
        }

        _logger.LogInformation("2FA disabled for user {UserId}", userId);

        return Result.Success();
    }

    /// <summary>
    /// Regenerates backup codes.
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> RegenerateBackupCodesAsync(
        Guid userId,
        string totpCode,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyList<string>>(IdentityErrors.UserNotFound);

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result.Failure<IReadOnlyList<string>>(IdentityErrors.TwoFactorNotEnabled);

        // Verify TOTP code
        if (!_twoFactorService.ValidateTotpCode(user.TwoFactorSecret, totpCode))
            return Result.Failure<IReadOnlyList<string>>(IdentityErrors.InvalidTwoFactorCode);

        // Delete old codes
        await _backupCodeRepository.DeleteAllForUserAsync(userId, cancellationToken);

        // Generate new codes
        var backupCodePairs = TwoFactorBackupCode.CreateMultiple(userId, _settings.BackupCodeCount);
        var rawCodes = new List<string>(backupCodePairs.Count);
        
        foreach (var (code, rawCode) in backupCodePairs)
        {
            await _backupCodeRepository.AddAsync(code, cancellationToken);
            rawCodes.Add(rawCode);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Backup codes regenerated for user {UserId}", userId);

        return Result.Success<IReadOnlyList<string>>(rawCodes.AsReadOnly());
    }

    /// <summary>
    /// Gets the count of remaining backup codes.
    /// </summary>
    public async Task<int> GetRemainingBackupCodeCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _backupCodeRepository.GetUnusedCodeCountAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Checks if 2FA is enabled for a user.
    /// </summary>
    public async Task<bool> IsTwoFactorEnabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.TwoFactorEnabled ?? false;
    }

    #region Private Methods

    private static string GenerateSetupToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static void CleanupExpiredSetupTokens()
    {
        var expiredTokens = _setupTokens
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _setupTokens.Remove(token);
        }
    }

    private sealed class TwoFactorSetupData
    {
        public Guid UserId { get; init; }
        public string Secret { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }

    #endregion
}
