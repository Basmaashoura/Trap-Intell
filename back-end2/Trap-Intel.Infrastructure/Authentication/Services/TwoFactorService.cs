using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for Two-Factor Authentication using TOTP (RFC 6238).
/// Provides secure TOTP generation, validation, and backup code management.
/// </summary>
public sealed class TwoFactorService : ITwoFactorService
{
    private const int SecretSizeBytes = 20; // 160 bits, standard for TOTP
    private const int BackupCodeLength = 8;
    private const string BackupCodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing chars

    private readonly TwoFactorSettings _settings;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        IOptions<TwoFactorSettings> settings,
        ILogger<TwoFactorService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new TOTP secret and QR code for 2FA setup.
    /// </summary>
    public TwoFactorSetupInfo GenerateSetupInfo(string userEmail, string? userName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);

        // Generate cryptographically secure random secret
        var secretBytes = RandomNumberGenerator.GetBytes(SecretSizeBytes);
        var base32Secret = Base32Encoding.ToString(secretBytes);

        // Format secret with spaces for manual entry (every 4 characters)
        var formattedSecret = FormatSecretForDisplay(base32Secret);

        // Build OTP Auth URI (RFC 6238 / Google Authenticator compatible)
        var accountName = Uri.EscapeDataString(userEmail);
        var issuer = Uri.EscapeDataString(_settings.Issuer);
        
        var otpAuthUri = $"otpauth://totp/{issuer}:{accountName}" +
                         $"?secret={base32Secret}" +
                         $"&issuer={issuer}" +
                         $"&algorithm=SHA1" +
                         $"&digits={_settings.TotpDigits}" +
                         $"&period={_settings.TotpTimeStepSeconds}";

        // Generate QR code
        var qrCodeBase64 = GenerateQrCode(otpAuthUri);

        _logger.LogDebug("Generated 2FA setup info for account: {AccountName}", userEmail);

        return new TwoFactorSetupInfo
        {
            Secret = base32Secret,
            SecretFormatted = formattedSecret,
            OtpAuthUri = otpAuthUri,
            QrCodeBase64 = qrCodeBase64,
            Issuer = _settings.Issuer,
            AccountName = userEmail
        };
    }

    /// <summary>
    /// Validates a TOTP code against a secret with time window tolerance.
    /// </summary>
    public bool ValidateTotpCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            // Normalize code (remove spaces, ensure digits only)
            var normalizedCode = NormalizeCode(code);
            
            if (normalizedCode.Length != _settings.TotpDigits)
                return false;

            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(
                secretBytes,
                step: _settings.TotpTimeStepSeconds,
                totpSize: _settings.TotpDigits);

            // Verify with time window tolerance
            var window = new VerificationWindow(
                previous: _settings.TotpTimeTolerance,
                future: _settings.TotpTimeTolerance);

            return totp.VerifyTotp(normalizedCode, out _, window);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TOTP validation failed due to exception");
            return false;
        }
    }

    /// <summary>
    /// Generates backup codes for recovery.
    /// </summary>
    public IReadOnlyList<string> GenerateBackupCodes(int? count = null)
    {
        var codeCount = count ?? _settings.BackupCodeCount;
        
        if (codeCount < 1 || codeCount > 20)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and 20.");

        var codes = new List<string>(codeCount);
        
        for (int i = 0; i < codeCount; i++)
        {
            codes.Add(GenerateBackupCode());
        }

        _logger.LogDebug("Generated {Count} backup codes", codeCount);
        return codes.AsReadOnly();
    }

    /// <summary>
    /// Validates a backup code against a hash using timing-safe comparison.
    /// </summary>
    public bool ValidateBackupCode(string codeHash, string providedCode)
    {
        if (string.IsNullOrWhiteSpace(codeHash) || string.IsNullOrWhiteSpace(providedCode))
            return false;

        var providedHash = HashBackupCode(providedCode);

        // Timing-safe comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(codeHash),
            Encoding.UTF8.GetBytes(providedHash));
    }

    /// <summary>
    /// Hashes a backup code using SHA-256.
    /// </summary>
    public string HashBackupCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = NormalizeBackupCode(code);
        var codeBytes = Encoding.UTF8.GetBytes(normalizedCode);
        var hashBytes = SHA256.HashData(codeBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the current TOTP code for a secret (for testing/debugging only).
    /// </summary>
    public string GetCurrentTotpCode(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(
            secretBytes,
            step: _settings.TotpTimeStepSeconds,
            totpSize: _settings.TotpDigits);

        return totp.ComputeTotp();
    }

    #region Private Methods

    /// <summary>
    /// Generates a single backup code.
    /// </summary>
    private static string GenerateBackupCode()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(BackupCodeLength);
        var code = new char[BackupCodeLength];
        
        for (int i = 0; i < BackupCodeLength; i++)
        {
            code[i] = BackupCodeCharacters[randomBytes[i] % BackupCodeCharacters.Length];
        }

        return new string(code);
    }

    /// <summary>
    /// Formats a Base32 secret for display with spaces every 4 characters.
    /// </summary>
    private static string FormatSecretForDisplay(string secret)
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < secret.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
                sb.Append(' ');
            sb.Append(secret[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes a TOTP code by removing spaces and non-digit characters.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return new string(code.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Normalizes a backup code by removing spaces/dashes and converting to uppercase.
    /// </summary>
    private static string NormalizeBackupCode(string code)
    {
        return code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
    }

    /// <summary>
    /// Generates a QR code as Base64-encoded PNG.
    /// Uses a simple implementation without external QR library dependency.
    /// In production, consider using QRCoder or SkiaSharp for better QR codes.
    /// </summary>
    private static string GenerateQrCode(string content)
    {
        // For production, integrate a proper QR code library like QRCoder
        // This returns a placeholder - the actual QR code generation should use:
        // using var qrGenerator = new QRCodeGenerator();
        // var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        // using var qrCode = new PngByteQRCode(qrCodeData);
        // return Convert.ToBase64String(qrCode.GetGraphic(5));

        // For now, return the OTP URI so frontend can generate QR code
        // Many frontend libraries (qrcode.js, etc.) can generate QR codes client-side
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }

    #endregion
}
