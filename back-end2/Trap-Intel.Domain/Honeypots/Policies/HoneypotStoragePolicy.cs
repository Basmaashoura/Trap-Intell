using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots.Policies;

/// <summary>
/// Policy object for honeypot storage management.
/// Encapsulates storage quota and usage calculations.
/// </summary>
public class HoneypotStoragePolicy
{
    private const decimal DEFAULT_WARNING_THRESHOLD_PERCENT = 80;

    /// <summary>
    /// Check if storage usage is approaching quota limit.
    /// </summary>
    public static bool IsStorageNearLimit(
        Honeypot honeypot,
        decimal quotaGb,
        decimal warningThresholdPercent = DEFAULT_WARNING_THRESHOLD_PERCENT)
    {
        if (quotaGb <= 0)
            return false;

        var usagePercent = CalculateStorageUsagePercent(honeypot, quotaGb);
        return usagePercent >= warningThresholdPercent;
    }

    /// <summary>
    /// Calculate storage usage percentage.
    /// </summary>
    public static decimal CalculateStorageUsagePercent(Honeypot honeypot, decimal quotaGb)
    {
        if (quotaGb <= 0)
            return 0;

        var usageGb = honeypot.Health.StorageUsedGb;
        return (usageGb / quotaGb) * 100;
    }

    /// <summary>
    /// Calculate remaining storage.
    /// </summary>
    public static decimal CalculateRemainingStorageGb(Honeypot honeypot, decimal quotaGb)
    {
        var usageGb = honeypot.Health.StorageUsedGb;
        var remaining = quotaGb - usageGb;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Get storage status summary.
    /// </summary>
    public static StorageStatus GetStorageStatus(Honeypot honeypot, decimal quotaGb)
    {
        var usagePercent = CalculateStorageUsagePercent(honeypot, quotaGb);
        var remainingGb = CalculateRemainingStorageGb(honeypot, quotaGb);

        return new StorageStatus
        {
            UsedGb = honeypot.Health.StorageUsedGb,
            QuotaGb = quotaGb,
            UsagePercent = usagePercent,
            RemainingGb = remainingGb,
            Level = DetermineStorageLevel(usagePercent)
        };
    }

    /// <summary>
    /// Validate storage update.
    /// </summary>
    public static Result ValidateStorageUpdate(long newStorageBytes)
    {
        if (newStorageBytes < 0)
            return Result.Failure(
                Error.Custom("HoneypotStorage.InvalidValue", 
                    "Storage value cannot be negative"));

        return Result.Success();
    }

    #region Private Helpers

    private static StorageLevel DetermineStorageLevel(decimal usagePercent)
    {
        return usagePercent switch
        {
            >= 95 => StorageLevel.Critical,
            >= 80 => StorageLevel.Warning,
            >= 60 => StorageLevel.Moderate,
            _ => StorageLevel.Normal
        };
    }

    #endregion
}

/// <summary>
/// Storage status summary.
/// </summary>
public class StorageStatus
{
    public decimal UsedGb { get; set; }
    public decimal QuotaGb { get; set; }
    public decimal UsagePercent { get; set; }
    public decimal RemainingGb { get; set; }
    public StorageLevel Level { get; set; }

    public bool IsNearLimit => Level == StorageLevel.Warning || Level == StorageLevel.Critical;
    public bool IsCritical => Level == StorageLevel.Critical;
}

/// <summary>
/// Storage usage level.
/// </summary>
public enum StorageLevel
{
    Normal = 0,
    Moderate = 1,
    Warning = 2,
    Critical = 3
}
