using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for detecting and analyzing quota violations.
    /// 
    /// SINGLE RESPONSIBILITY: Detect violations and determine quota status.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (violation detection)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only violation detection)
    /// 
    /// Lines: ~170 (SOLID-compliant)
    /// </summary>
    public class QuotaViolationDetector
    {
        private readonly QuotaChecker _quotaChecker;

        // Business rule constants
        private const decimal WarningThresholdPercent = 75m;
        private const decimal CriticalThresholdPercent = 90m;
        private const decimal ExceededThresholdPercent = 100m;

        public QuotaViolationDetector(QuotaChecker quotaChecker)
        {
            _quotaChecker = quotaChecker ?? throw new ArgumentNullException(nameof(quotaChecker));
        }

        /// <summary>
        /// Detect all quota violations.
        /// Returns empty list if no violations.
        /// </summary>
        public List<QuotaViolation> DetectViolations(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var violations = new List<QuotaViolation>();

            // Check honeypot quota
            if (usage.HoneypotsUsed > quota.MaxHoneypots)
            {
                violations.Add(CreateViolation(
                    QuotaType.Honeypots,
                    usage.HoneypotsUsed,
                    quota.MaxHoneypots,
                    usage.HoneypotsUsed - quota.MaxHoneypots,
                    quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed)));
            }

            // Check storage quota
            if (usage.StorageUsedGb > quota.MaxStorageGb)
            {
                violations.Add(CreateViolation(
                    QuotaType.Storage,
                    usage.StorageUsedGb,
                    quota.MaxStorageGb,
                    usage.StorageUsedGb - quota.MaxStorageGb,
                    quota.GetStorageUsagePercentage(usage.StorageUsedGb)));
            }

            return violations;
        }

        /// <summary>
        /// Get overall quota status based on usage percentage.
        /// 
        /// BUSINESS RULES:
        /// - Normal: < 75%
        /// - Warning: 75-89%
        /// - Critical: 90-99%
        /// - Exceeded: >= 100%
        /// </summary>
        public QuotaStatus GetQuotaStatus(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var maxPercent = _quotaChecker.GetMaxUsagePercentage(usage, quota);
            return DetermineStatus(maxPercent);
        }

        /// <summary>
        /// Generate recommendations based on status and violations.
        /// </summary>
        public List<string> GenerateRecommendations(
            QuotaStatus status,
            List<QuotaViolation> violations)
        {
            if (violations == null)
                throw new ArgumentNullException(nameof(violations));

            var recommendations = new List<string>();

            switch (status)
            {
                case QuotaStatus.Exceeded:
                    recommendations.Add("?? CRITICAL: Quota exceeded. Upgrade plan immediately or reduce usage.");
                    foreach (var violation in violations)
                    {
                        recommendations.Add($"• Reduce {violation.ResourceType} by {violation.ExcessAmount:F2}");
                    }
                    break;

                case QuotaStatus.Critical:
                    recommendations.Add("?? WARNING: Approaching quota limits (90%+). Plan upgrade recommended.");
                    recommendations.Add("• Monitor usage closely");
                    recommendations.Add("• Consider upgrading to higher plan");
                    break;

                case QuotaStatus.Warning:
                    recommendations.Add("?? Usage is at 75%+ of quota. Monitor and plan for growth.");
                    break;

                case QuotaStatus.Normal:
                    recommendations.Add("? Usage is healthy. Continue normal operations.");
                    break;
            }

            return recommendations;
        }

        #region Private Helper Methods

        private QuotaViolation CreateViolation(
            QuotaType resourceType,
            decimal currentValue,
            decimal maxValue,
            decimal excessAmount,
            decimal percentageUsed)
        {
            var severity = DetermineSeverity(percentageUsed);

            return new QuotaViolation(
                resourceType,
                currentValue,
                maxValue,
                excessAmount,
                percentageUsed,
                severity);
        }

        private QuotaSeverity DetermineSeverity(decimal percentageUsed)
        {
            return percentageUsed switch
            {
                >= ExceededThresholdPercent => QuotaSeverity.Exceeded,
                >= CriticalThresholdPercent => QuotaSeverity.Critical,
                >= WarningThresholdPercent => QuotaSeverity.Warning,
                _ => QuotaSeverity.Normal
            };
        }

        private QuotaStatus DetermineStatus(decimal percentage)
        {
            return percentage switch
            {
                >= ExceededThresholdPercent => QuotaStatus.Exceeded,
                >= CriticalThresholdPercent => QuotaStatus.Critical,
                >= WarningThresholdPercent => QuotaStatus.Warning,
                _ => QuotaStatus.Normal
            };
        }

        #endregion
    }
}
