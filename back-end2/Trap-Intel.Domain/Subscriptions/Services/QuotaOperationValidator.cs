using System;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for validating if operations can be performed within quota.
    /// 
    /// SINGLE RESPONSIBILITY: Validate operations against quota limits.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (operation validation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only operation validation)
    /// 
    /// Lines: ~140 (SOLID-compliant)
    /// </summary>
    public class QuotaOperationValidator
    {
        private readonly QuotaChecker _quotaChecker;

        public QuotaOperationValidator(QuotaChecker quotaChecker)
        {
            _quotaChecker = quotaChecker ?? throw new ArgumentNullException(nameof(quotaChecker));
        }

        /// <summary>
        /// Check if adding additional usage would exceed quota.
        /// Proactive validation before performing operations.
        /// </summary>
        public bool WouldExceedQuota(
            UsageStatistics usage,
            SubscriptionQuota quota,
            int additionalHoneypots = 0,
            decimal additionalStorageGb = 0)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            if (additionalHoneypots < 0)
                throw new ArgumentException("Additional honeypots cannot be negative.", nameof(additionalHoneypots));

            if (additionalStorageGb < 0)
                throw new ArgumentException("Additional storage cannot be negative.", nameof(additionalStorageGb));

            var projectedHoneypots = usage.HoneypotsUsed + additionalHoneypots;
            var projectedStorage = usage.StorageUsedGb + additionalStorageGb;

            return projectedHoneypots > quota.MaxHoneypots ||
                   projectedStorage > quota.MaxStorageGb;
        }

        /// <summary>
        /// Validate if a specific operation can be performed.
        /// </summary>
        public QuotaOperationValidation ValidateOperation(
            UsageStatistics usage,
            SubscriptionQuota quota,
            QuotaOperation operation)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            return operation switch
            {
                QuotaOperation.CreateHoneypot =>
                    ValidateCreateHoneypot(usage, quota),

                QuotaOperation.UploadLogs =>
                    ValidateStorageOperation(usage, quota, 1m), // 1GB

                QuotaOperation.ScaleUp =>
                    ValidateScaleUp(usage, quota, 5), // 5 honeypots

                _ => new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: "Unknown operation type",
                    RemainingCapacity: 0)
            };
        }

        #region Private Operation Validators

        private QuotaOperationValidation ValidateCreateHoneypot(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalHoneypots: 1);

            if (wouldExceed)
            {
                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Cannot create honeypot. Quota: {usage.HoneypotsUsed}/{quota.MaxHoneypots}. " +
                            $"Upgrade plan or remove existing honeypots.",
                    RemainingCapacity: 0);
            }

            var remaining = (int)_quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Honeypots);

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: $"Honeypot creation allowed. {remaining} slot(s) available.",
                RemainingCapacity: remaining);
        }

        private QuotaOperationValidation ValidateStorageOperation(
            UsageStatistics usage,
            SubscriptionQuota quota,
            decimal requiredStorageGb)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalStorageGb: requiredStorageGb);

            if (wouldExceed)
            {
                var available = _quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Storage);
                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Insufficient storage. Need {requiredStorageGb:F2}GB, available {available:F2}GB",
                    RemainingCapacity: available);
            }

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: "Storage operation allowed",
                RemainingCapacity: _quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Storage));
        }

        private QuotaOperationValidation ValidateScaleUp(
            UsageStatistics usage,
            SubscriptionQuota quota,
            int additionalHoneypots)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalHoneypots: additionalHoneypots);

            if (wouldExceed)
            {
                var available = (int)_quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Honeypots);

                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Cannot scale up by {additionalHoneypots}. Only {available} slot(s) available.",
                    RemainingCapacity: available);
            }

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: $"Scale up by {additionalHoneypots} allowed",
                RemainingCapacity: (int)_quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Honeypots));
        }

        #endregion
    }
}
