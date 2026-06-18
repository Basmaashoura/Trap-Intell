using System;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Value objects for the Subscriptions domain.
    /// </summary>

    /// <summary>
    /// Represents subscription usage statistics.
    /// </summary>
    public record UsageStatistics(
        int HoneypotsUsed,
        decimal StorageUsedGb,
        decimal OverageCharges = 0);

    /// <summary>
    /// Represents billing information for a subscription.
    /// </summary>
    public record BillingInfo(
        BillingCycle Cycle,
        decimal TotalBilled,
        decimal? DiscountApplied = null);

    /// <summary>
    /// Represents subscription period.
    /// </summary>
    public record SubscriptionPeriod(
        DateTime StartDate,
        DateTime? EndDate = null,
        DateTime? RenewalDate = null);

    /// <summary>
    /// Represents cancellation information.
    /// </summary>
    public record CancellationInfo(
        DateTime CancelledAt,
        string Reason);

    /// <summary>
    /// Summary of quota usage for display and reporting.
    /// </summary>
    public record QuotaUsageSummary(
        int CurrentHoneypots,
        int MaxHoneypots,
        decimal HoneypotUsagePercent,
        decimal CurrentStorageGb,
        decimal MaxStorageGb,
        decimal StorageUsagePercent,
        int CurrentApiCalls,
        int MaxApiCalls,
        decimal ApiCallsUsagePercent)
    {
        /// <summary>
        /// Check if any resource is over limit.
        /// </summary>
        public bool IsOverLimit => 
            HoneypotUsagePercent > 100 || 
            StorageUsagePercent > 100 || 
            ApiCallsUsagePercent > 100;

        /// <summary>
        /// Check if any resource is approaching limit (>80%).
        /// </summary>
        public bool IsApproachingLimit => 
            HoneypotUsagePercent >= 80 || 
            StorageUsagePercent >= 80 || 
            ApiCallsUsagePercent >= 80;

        /// <summary>
        /// Get the highest usage percentage across all resources.
        /// </summary>
        public decimal HighestUsagePercent => 
            Math.Max(Math.Max(HoneypotUsagePercent, StorageUsagePercent), ApiCallsUsagePercent);

        /// <summary>
        /// Get remaining honeypots.
        /// </summary>
        public int RemainingHoneypots => Math.Max(0, MaxHoneypots - CurrentHoneypots);

        /// <summary>
        /// Get remaining storage.
        /// </summary>
        public decimal RemainingStorageGb => Math.Max(0, MaxStorageGb - CurrentStorageGb);

        /// <summary>
        /// Get remaining API calls.
        /// </summary>
        public int RemainingApiCalls => Math.Max(0, MaxApiCalls - CurrentApiCalls);
    }
}
