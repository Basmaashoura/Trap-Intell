namespace Infrastructure.Identity;

/// <summary>
/// Configuration settings for token cleanup.
/// </summary>
public sealed class TokenCleanupSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "TokenCleanup";

    /// <summary>
    /// Whether token cleanup is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Interval between cleanup runs in minutes.
    /// Default: 60 minutes (1 hour).
    /// </summary>
    public int IntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Batch size for cleanup operations.
    /// Default: 1000 records per batch.
    /// </summary>
    public int BatchSize { get; init; } = 1000;
}
