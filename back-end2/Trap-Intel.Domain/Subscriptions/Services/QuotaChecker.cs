using System;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for basic quota checking operations.
    /// 
    /// SINGLE RESPONSIBILITY: Check if usage is within quota limits.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (quota checks)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only quota checks)
    /// 
    /// Lines: ~120 (SOLID-compliant)
    /// </summary>
    public class QuotaChecker
    {
        /// <summary>
        /// Check if usage is within all quota limits.
        /// 
        /// BUSINESS RULE:
        /// All resource types must be within their respective quotas.
        /// </summary>
        public bool IsWithinQuota(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            return usage.HoneypotsUsed <= quota.MaxHoneypots &&
                   usage.StorageUsedGb <= quota.MaxStorageGb;
        }

        /// <summary>
        /// Check if a specific resource is within quota.
        /// </summary>
        public bool IsResourceWithinQuota(
            UsageStatistics usage,
            SubscriptionQuota quota,
            QuotaType resourceType)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            return resourceType switch
            {
                QuotaType.Honeypots => usage.HoneypotsUsed <= quota.MaxHoneypots,
                QuotaType.Storage => usage.StorageUsedGb <= quota.MaxStorageGb,
                _ => false
            };
        }

        /// <summary>
        /// Get available quota for a resource type.
        /// Returns 0 if quota is exceeded.
        /// </summary>
        public decimal GetAvailableQuota(
            UsageStatistics usage,
            SubscriptionQuota quota,
            QuotaType resourceType)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            return resourceType switch
            {
                QuotaType.Honeypots => Math.Max(0, quota.MaxHoneypots - usage.HoneypotsUsed),
                QuotaType.Storage => Math.Max(0, quota.MaxStorageGb - usage.StorageUsedGb),
                _ => 0
            };
        }

        /// <summary>
        /// Get usage percentage for a resource type.
        /// </summary>
        public decimal GetUsagePercentage(
            UsageStatistics usage,
            SubscriptionQuota quota,
            QuotaType resourceType)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            return resourceType switch
            {
                QuotaType.Honeypots => quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed),
                QuotaType.Storage => quota.GetStorageUsagePercentage(usage.StorageUsedGb),
                _ => 0
            };
        }

        /// <summary>
        /// Get maximum usage percentage across all resource types.
        /// Used to determine overall quota status.
        /// </summary>
        public decimal GetMaxUsagePercentage(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var honeypotPercent = quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed);
            var storagePercent = quota.GetStorageUsagePercentage(usage.StorageUsedGb);

            return Math.Max(honeypotPercent, storagePercent);
        }
    }
}
