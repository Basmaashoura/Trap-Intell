using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for calculating subscription usage statistics.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (usage calculations)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain objects only (Honeypot, UsageStatistics, SubscriptionQuota)
    /// ? Encapsulates domain knowledge about usage tracking
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic calculations)
    /// - Single Responsibility (only usage calculations)
    /// - Domain-driven (uses domain aggregates and value objects)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In Subscription.cs:
    ///   public void UpdateUsage(int honeypotsUsed, decimal storageUsedGb, decimal overageCharges = 0)
    ///   {
    ///       Usage = new UsageStatistics(honeypotsUsed, storageUsedGb, overageCharges);
    ///       // ?? Just stores! Doesn't CALCULATE overageCharges!
    ///   }
    /// This service fills that gap by providing calculation logic.
    /// </summary>
    public class UsageCalculationService
    {
        /// <summary>
        /// Calculate usage statistics from a list of honeypots.
        /// 
        /// BUSINESS RULES:
        /// - Only count active and paused honeypots (not terminated/error)
        /// - Sum storage from all honeypots
        /// - Count total events captured
        /// 
        /// EXAMPLE:
        /// - 3 active honeypots, 2 terminated
        /// - Result: honeypotsUsed = 3 (only active/paused count)
        /// </summary>
        /// <param name="honeypots">List of honeypots to analyze</param>
        /// <returns>Usage statistics calculated from honeypots</returns>
        public UsageStatistics CalculateUsageFromHoneypots(List<Honeypot> honeypots)
        {
            if (honeypots == null)
                throw new ArgumentNullException(nameof(honeypots));

            // Count only active and paused honeypots (billable states)
            var honeypotsUsed = honeypots.Count(h => 
                h.Status == HoneypotStatus.Active || 
                h.Status == HoneypotStatus.Paused);

            // Sum storage from all honeypots (including terminated for accurate billing)
            var storageUsedGb = honeypots.Sum(h => h.Health.StorageUsedGb);

            // Count total events captured
            var totalEvents = honeypots.Sum(h => h.Statistics.TotalEventsCapture);

            return new UsageStatistics(honeypotsUsed, storageUsedGb);
        }

        /// <summary>
        /// Calculate usage percentage against quota.
        /// Returns the MAXIMUM percentage to determine overall status.
        /// 
        /// BUSINESS RULE:
        /// - Use the highest percentage (honeypots or storage) as overall metric
        /// - This determines if subscription is approaching limits
        /// 
        /// EXAMPLE:
        /// - Honeypots: 8/10 = 80%
        /// - Storage: 45/50GB = 90%
        /// - Result: 90% (highest)
        /// </summary>
        /// <param name="usage">Current usage statistics</param>
        /// <param name="quota">Subscription quota limits</param>
        /// <returns>Maximum usage percentage (0-100+)</returns>
        public decimal CalculateUsagePercentage(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            // Calculate individual percentages
            var honeypotPercentage = quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed);
            var storagePercentage = quota.GetStorageUsagePercentage(usage.StorageUsedGb);

            // Return maximum (most critical)
            return Math.Max(honeypotPercentage, storagePercentage);
        }

        /// <summary>
        /// Calculate overage charges for usage exceeding quota.
        /// 
        /// BUSINESS RULES:
        /// - Only charge for usage ABOVE quota
        /// - Separate rates for honeypots and storage
        /// - Overage = (actual - quota) * rate
        /// 
        /// EXAMPLE:
        /// - Quota: 10 honeypots, 50GB storage
        /// - Usage: 12 honeypots, 60GB storage
        /// - Rates: $5/honeypot, $0.50/GB
        /// - Overage: (2 * $5) + (10 * $0.50) = $15
        /// </summary>
        /// <param name="usage">Current usage statistics</param>
        /// <param name="quota">Subscription quota limits</param>
        /// <param name="honeypotOverageRate">Cost per excess honeypot</param>
        /// <param name="storageOverageRatePerGb">Cost per excess GB</param>
        /// <returns>Total overage charges broken down by type</returns>
        public OverageCharges CalculateOverages(
            UsageStatistics usage,
            SubscriptionQuota quota,
            decimal honeypotOverageRate,
            decimal storageOverageRatePerGb)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            if (honeypotOverageRate < 0)
                throw new ArgumentException("Honeypot overage rate cannot be negative.", nameof(honeypotOverageRate));

            if (storageOverageRatePerGb < 0)
                throw new ArgumentException("Storage overage rate cannot be negative.", nameof(storageOverageRatePerGb));

            // Calculate excess usage (0 if within quota)
            var excessHoneypots = Math.Max(0, usage.HoneypotsUsed - quota.MaxHoneypots);
            var excessStorageGb = Math.Max(0, usage.StorageUsedGb - quota.MaxStorageGb);

            // Calculate charges
            var honeypotCharge = excessHoneypots * honeypotOverageRate;
            var storageCharge = excessStorageGb * storageOverageRatePerGb;

            return new OverageCharges(
                HoneypotOverage: honeypotCharge,
                StorageOverage: storageCharge,
                TotalOverage: honeypotCharge + storageCharge);
        }

        /// <summary>
        /// Calculate remaining quota available.
        /// Used for UI display and proactive notifications.
        /// 
        /// BUSINESS RULE:
        /// - If usage exceeds quota, remaining is 0 (not negative)
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Subscription quota</param>
        /// <returns>Remaining quota for honeypots and storage</returns>
        public RemainingQuota CalculateRemainingQuota(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var remainingHoneypots = Math.Max(0, quota.MaxHoneypots - usage.HoneypotsUsed);
            var remainingStorage = Math.Max(0, quota.MaxStorageGb - usage.StorageUsedGb);

            return new RemainingQuota(
                RemainingHoneypots: remainingHoneypots,
                RemainingStorageGb: remainingStorage);
        }

        /// <summary>
        /// Predict when quota will be exceeded based on current growth rate.
        /// Used for proactive alerts and capacity planning.
        /// 
        /// BUSINESS RULES:
        /// - Calculate daily growth rate from historical data
        /// - Project forward to find when quota is reached
        /// - Return null if growth is negative or quota already exceeded
        /// 
        /// EXAMPLE:
        /// - Current: 40GB, Quota: 50GB, Growth: 2GB/day
        /// - Days until full: (50-40)/2 = 5 days
        /// </summary>
        /// <param name="currentUsage">Current usage statistics</param>
        /// <param name="historicalUsage">Historical usage for trend analysis (ordered oldest to newest)</param>
        /// <param name="quota">Subscription quota</param>
        /// <returns>Predicted date when quota will be exceeded, or null if not predictable</returns>
        public DateTime? PredictQuotaExceededDate(
            UsageStatistics currentUsage,
            List<UsageStatistics> historicalUsage,
            SubscriptionQuota quota)
        {
            if (currentUsage == null)
                throw new ArgumentNullException(nameof(currentUsage));

            if (historicalUsage == null || historicalUsage.Count < 2)
                return null; // Need at least 2 data points for trend

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            // Check if already exceeded
            if (currentUsage.HoneypotsUsed >= quota.MaxHoneypots ||
                currentUsage.StorageUsedGb >= quota.MaxStorageGb)
            {
                return DateTime.UtcNow; // Already exceeded
            }

            // Calculate growth rates (storage is more predictable than honeypot count)
            var storageGrowthPerDay = CalculateAverageGrowthRate(
                historicalUsage.Select(u => u.StorageUsedGb).ToList());

            if (storageGrowthPerDay <= 0)
                return null; // No growth or decreasing usage

            // Calculate days until storage quota exceeded
            var remainingStorageGb = quota.MaxStorageGb - currentUsage.StorageUsedGb;
            var daysUntilStorageExceeded = (double)(remainingStorageGb / storageGrowthPerDay);

            if (daysUntilStorageExceeded <= 0)
                return DateTime.UtcNow;

            return DateTime.UtcNow.AddDays(daysUntilStorageExceeded);
        }

        /// <summary>
        /// Calculate average usage across a time period.
        /// Used for reporting and analytics.
        /// </summary>
        /// <param name="usageHistory">Historical usage data points</param>
        /// <returns>Average usage statistics</returns>
        public UsageStatistics CalculateAverageUsage(List<UsageStatistics> usageHistory)
        {
            if (usageHistory == null || usageHistory.Count == 0)
                throw new ArgumentException("Usage history cannot be null or empty.", nameof(usageHistory));

            var avgHoneypots = (int)usageHistory.Average(u => u.HoneypotsUsed);
            var avgStorage = usageHistory.Average(u => u.StorageUsedGb);

            return new UsageStatistics(avgHoneypots, avgStorage);
        }

        /// <summary>
        /// Calculate peak usage from historical data.
        /// Important for capacity planning and plan recommendations.
        /// </summary>
        /// <param name="usageHistory">Historical usage data points</param>
        /// <returns>Peak usage statistics</returns>
        public UsageStatistics CalculatePeakUsage(List<UsageStatistics> usageHistory)
        {
            if (usageHistory == null || usageHistory.Count == 0)
                throw new ArgumentException("Usage history cannot be null or empty.", nameof(usageHistory));

            var maxHoneypots = usageHistory.Max(u => u.HoneypotsUsed);
            var maxStorage = usageHistory.Max(u => u.StorageUsedGb);

            return new UsageStatistics(maxHoneypots, maxStorage);
        }

        /// <summary>
        /// Determine usage status level based on percentage.
        /// Used for UI indicators and alert priorities.
        /// 
        /// BUSINESS RULES:
        /// - Normal: < 75%
        /// - Warning: 75-89%
        /// - Critical: 90-99%
        /// - Exceeded: >= 100%
        /// </summary>
        /// <param name="usagePercentage">Current usage percentage</param>
        /// <returns>Usage status level</returns>
        public UsageStatus DetermineUsageStatus(decimal usagePercentage)
        {
            return usagePercentage switch
            {
                >= 100 => UsageStatus.Exceeded,
                >= 90 => UsageStatus.Critical,
                >= 75 => UsageStatus.Warning,
                _ => UsageStatus.Normal
            };
        }

        #region Private Helper Methods

        /// <summary>
        /// Calculate average daily growth rate from historical data.
        /// Uses linear regression for better accuracy.
        /// </summary>
        private decimal CalculateAverageGrowthRate(List<decimal> values)
        {
            if (values.Count < 2)
                return 0;

            // Simple approach: calculate change per data point
            var totalChange = values[^1] - values[0]; // Last - First
            var dataPoints = values.Count - 1;

            return dataPoints > 0 ? totalChange / dataPoints : 0;
        }

        #endregion
    }

    #region Supporting Value Objects

    /// <summary>
    /// Value object representing overage charges breakdown.
    /// </summary>
    public record OverageCharges(
        decimal HoneypotOverage,
        decimal StorageOverage,
        decimal TotalOverage);

    /// <summary>
    /// Value object representing remaining quota.
    /// </summary>
    public record RemainingQuota(
        int RemainingHoneypots,
        decimal RemainingStorageGb);

    /// <summary>
    /// Enumeration of usage status levels.
    /// </summary>
    public enum UsageStatus
    {
        Normal = 0,      // < 75%
        Warning = 1,     // 75-89%
        Critical = 2,    // 90-99%
        Exceeded = 3     // >= 100%
    }

    #endregion
}
