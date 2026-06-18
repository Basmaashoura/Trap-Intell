using System.Security.Cryptography;

namespace Infrastructure.Identity;

/// <summary>
/// Provides security utilities for authentication operations.
/// </summary>
internal static class SecurityHelper
{
    /// <summary>
    /// Introduces a constant delay to prevent timing attacks on authentication.
    /// This makes it harder for attackers to enumerate valid emails by measuring response times.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the delay.</returns>
    public static async Task ConstantTimeDelayAsync(CancellationToken cancellationToken = default)
    {
        // Add a random delay between 50-150ms to prevent timing analysis
        // This makes response times consistent whether user exists or not
        int delayMs = RandomNumberGenerator.GetInt32(50, 150);
        await Task.Delay(delayMs, cancellationToken);
    }

    /// <summary>
    /// Introduces a fixed delay for security-sensitive operations.
    /// </summary>
    /// <param name="milliseconds">Delay in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the delay.</returns>
    public static async Task FixedDelayAsync(int milliseconds, CancellationToken cancellationToken = default)
    {
        await Task.Delay(milliseconds, cancellationToken);
    }
}
