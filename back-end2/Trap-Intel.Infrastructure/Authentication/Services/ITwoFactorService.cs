using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Interface for Two-Factor Authentication service.
/// Handles TOTP generation, validation, and backup code management.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new TOTP secret and QR code for 2FA setup.
    /// </summary>
    /// <param name="userEmail">The user's email address (shown in authenticator app).</param>
    /// <param name="userName">Optional display name for the user.</param>
    /// <returns>Setup information including secret and QR code.</returns>
    TwoFactorSetupInfo GenerateSetupInfo(string userEmail, string? userName = null);

    /// <summary>
    /// Validates a TOTP code against a secret.
    /// </summary>
    /// <param name="secret">The Base32-encoded TOTP secret.</param>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code is valid.</returns>
    bool ValidateTotpCode(string secret, string code);

    /// <summary>
    /// Generates backup codes for recovery.
    /// </summary>
    /// <param name="count">Number of codes to generate (default from settings).</param>
    /// <returns>List of plaintext backup codes (only returned once, must be shown to user).</returns>
    IReadOnlyList<string> GenerateBackupCodes(int? count = null);

    /// <summary>
    /// Validates a backup code against a hash.
    /// </summary>
    /// <param name="codeHash">The stored hash of the backup code.</param>
    /// <param name="providedCode">The code provided by the user.</param>
    /// <returns>True if the code matches.</returns>
    bool ValidateBackupCode(string codeHash, string providedCode);

    /// <summary>
    /// Hashes a backup code for storage.
    /// </summary>
    /// <param name="code">The plaintext backup code.</param>
    /// <returns>SHA-256 hash of the code.</returns>
    string HashBackupCode(string code);

    /// <summary>
    /// Gets the current TOTP code for a secret (for testing/debugging only).
    /// </summary>
    /// <param name="secret">The Base32-encoded TOTP secret.</param>
    /// <returns>The current TOTP code.</returns>
    string GetCurrentTotpCode(string secret);
}

/// <summary>
/// Contains information needed for 2FA setup.
/// </summary>
public sealed record TwoFactorSetupInfo
{
    /// <summary>
    /// The Base32-encoded secret key.
    /// Must be stored securely after user confirms setup.
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// The secret formatted with spaces for manual entry (e.g., "JBSW Y3DP EHPK 3PXP").
    /// </summary>
    public required string SecretFormatted { get; init; }

    /// <summary>
    /// The OTP Auth URI (otpauth://totp/...).
    /// Can be used to generate QR codes or deep link to authenticator apps.
    /// </summary>
    public required string OtpAuthUri { get; init; }

    /// <summary>
    /// Base64-encoded PNG image of the QR code.
    /// Ready to be displayed as an image in HTML: data:image/png;base64,{QrCodeBase64}
    /// </summary>
    public required string QrCodeBase64 { get; init; }

    /// <summary>
    /// The issuer name (application name).
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// The account name (usually email).
    /// </summary>
    public required string AccountName { get; init; }
}
