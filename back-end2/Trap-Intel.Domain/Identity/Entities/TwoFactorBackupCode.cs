using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents a backup code for two-factor authentication recovery.
/// Each code is single-use and provides emergency access when TOTP is unavailable.
/// </summary>
public sealed class TwoFactorBackupCode : Entity<Guid>
{
    private const int CodeLength = 8;
    private const string AllowedCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing chars (0,O,1,I,L)

    /// <summary>
    /// The user this backup code belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the backup code. Never store raw codes!
    /// </summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// When this code was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Whether this code has been used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When this code was used (null if not used).
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>
    /// IP address where the code was used (for security auditing).
    /// </summary>
    public string? UsedFromIp { get; private set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; private set; }

    // Private constructor for EF Core
    private TwoFactorBackupCode() { }

    /// <summary>
    /// Creates a new backup code.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Tuple of (BackupCode entity, raw code string for displaying to user).</returns>
    /// <exception cref="ArgumentException">Thrown when userId is empty.</exception>
    public static (TwoFactorBackupCode Code, string RawCode) Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        var rawCode = GenerateCode();
        var codeHash = HashCode(rawCode);

        var backupCode = new TwoFactorBackupCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        return (backupCode, rawCode);
    }

    /// <summary>
    /// Creates multiple backup codes for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="count">Number of codes to generate (default: 10).</param>
    /// <returns>List of tuples containing entities and raw codes.</returns>
    public static IReadOnlyList<(TwoFactorBackupCode Code, string RawCode)> CreateMultiple(
        Guid userId,
        int count = 10)
    {
        if (count < 1 || count > 20)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and 20.");

        var codes = new List<(TwoFactorBackupCode, string)>(count);
        
        for (int i = 0; i < count; i++)
        {
            codes.Add(Create(userId));
        }

        return codes.AsReadOnly();
    }

    /// <summary>
    /// Reconstructs a backup code from persistence.
    /// </summary>
    public static TwoFactorBackupCode Reconstruct(
        Guid id,
        Guid userId,
        string codeHash,
        DateTime createdAt,
        bool isUsed,
        DateTime? usedAt,
        string? usedFromIp)
    {
        return new TwoFactorBackupCode
        {
            Id = id,
            UserId = userId,
            CodeHash = codeHash,
            CreatedAt = createdAt,
            IsUsed = isUsed,
            UsedAt = usedAt,
            UsedFromIp = usedFromIp
        };
    }

    /// <summary>
    /// Validates a raw code against this backup code's hash using timing-safe comparison.
    /// </summary>
    /// <param name="rawCode">The raw code to validate.</param>
    /// <returns>True if the code matches and is not used.</returns>
    public bool ValidateCode(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode) || IsUsed)
            return false;

        var providedHash = HashCode(NormalizeCode(rawCode));
        
        // Timing-safe comparison
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(CodeHash),
            System.Text.Encoding.UTF8.GetBytes(providedHash));
    }

    /// <summary>
    /// Marks the backup code as used.
    /// </summary>
    /// <param name="ipAddress">IP address where the code was used.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result Use(string? ipAddress = null)
    {
        if (IsUsed)
            return Result.Failure(IdentityErrors.BackupCodeAlreadyUsed);

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedFromIp = SanitizeIpAddress(ipAddress);

        return Result.Success();
    }

    /// <summary>
    /// Generates a cryptographically secure random backup code.
    /// </summary>
    private static string GenerateCode()
    {
        var randomBytes = new byte[CodeLength];
        RandomNumberGenerator.Fill(randomBytes);
        
        var code = new char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
        {
            code[i] = AllowedCharacters[randomBytes[i] % AllowedCharacters.Length];
        }

        return new string(code);
    }

    /// <summary>
    /// Hashes a backup code using SHA-256.
    /// </summary>
    public static string HashCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        
        var normalized = NormalizeCode(code);
        var codeBytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        var hashBytes = SHA256.HashData(codeBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a code by removing spaces/dashes and converting to uppercase.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
    }

    /// <summary>
    /// Sanitizes IP address for safe storage.
    /// </summary>
    private static string? SanitizeIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        // Only allow valid IP characters
        var sanitized = new string(ip.Where(c => char.IsDigit(c) || c == '.' || c == ':').ToArray());
        return sanitized.Length > 45 ? sanitized[..45] : sanitized;
    }
}
