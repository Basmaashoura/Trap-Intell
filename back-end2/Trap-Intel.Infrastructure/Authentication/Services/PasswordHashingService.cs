namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Result of password verification.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>Password verification failed.</summary>
    Failed = 0,
    /// <summary>Password verification succeeded.</summary>
    Success = 1,
    /// <summary>Password verification succeeded but hash needs rehashing (upgraded algorithm).</summary>
    SuccessRehashNeeded = 2
}

/// <summary>
/// Service for secure password hashing using BCrypt.
/// BCrypt is OWASP recommended and provides adaptive hashing.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hash a password securely using BCrypt.
    /// </summary>
    /// <param name="password">Plain text password to hash.</param>
    /// <returns>BCrypt hash string.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a BCrypt hash.
    /// </summary>
    /// <param name="hashedPassword">The stored BCrypt hash.</param>
    /// <param name="providedPassword">The plain text password to verify.</param>
    /// <returns>Verification result indicating success, failure, or need for rehash.</returns>
    PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword);
}

/// <summary>
/// BCrypt password hashing service.
/// Uses work factor of 12 (OWASP recommended minimum is 10).
/// </summary>
public sealed class PasswordHashingService : IPasswordHashingService
{
    // Work factor 12 = 2^12 iterations = 4096 rounds
    // Takes ~250ms on modern hardware (good balance of security vs performance)
    // OWASP recommends minimum of 10
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
        
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword, nameof(hashedPassword));
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword, nameof(providedPassword));

        try
        {
            var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
            
            if (!isValid)
                return PasswordVerificationResult.Failed;

            // Check if rehash is needed (work factor increased)
            var hashInfo = BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, WorkFactor);
            
            return hashInfo 
                ? PasswordVerificationResult.SuccessRehashNeeded 
                : PasswordVerificationResult.Success;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Invalid hash format - treat as failed
            return PasswordVerificationResult.Failed;
        }
    }
}
