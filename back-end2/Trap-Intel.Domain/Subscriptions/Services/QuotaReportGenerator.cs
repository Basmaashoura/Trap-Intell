using System.Collections.Generic;
using System;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for generating comprehensive quota reports.
    /// 
    /// SINGLE RESPONSIBILITY: Generate detailed quota validation reports.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (report generation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only report generation)
    /// 
    /// Lines: ~120 (SOLID-compliant)
    /// </summary>
    public class QuotaReportGenerator
    {
        private readonly QuotaChecker _quotaChecker;
        private readonly QuotaViolationDetector _violationDetector;

        public QuotaReportGenerator(
            QuotaChecker quotaChecker,
            QuotaViolationDetector violationDetector)
        {
            _quotaChecker = quotaChecker ?? throw new ArgumentNullException(nameof(quotaChecker));
            _violationDetector = violationDetector ?? throw new ArgumentNullException(nameof(violationDetector));
        }

        /// <summary>
        /// Generate comprehensive quota validation report.
        /// Includes status, violations, usage, availability, and recommendations.
        /// </summary>
        public QuotaValidationReport GenerateReport(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            // Get all validation data
            var isWithinQuota = _quotaChecker.IsWithinQuota(usage, quota);
            var violations = _violationDetector.DetectViolations(usage, quota);
            var status = _violationDetector.GetQuotaStatus(usage, quota);

            // Get usage percentages
            var honeypotPercent = _quotaChecker.GetUsagePercentage(usage, quota, QuotaType.Honeypots);
            var storagePercent = _quotaChecker.GetUsagePercentage(usage, quota, QuotaType.Storage);

            // Get available quota
            var honeypotsAvailable = (int)_quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Honeypots);
            var storageAvailable = _quotaChecker.GetAvailableQuota(usage, quota, QuotaType.Storage);

            // Generate recommendations
            var recommendations = _violationDetector.GenerateRecommendations(status, violations);

            return new QuotaValidationReport(
                Status: status,
                IsWithinQuota: isWithinQuota,
                Violations: violations,
                HoneypotUsagePercent: honeypotPercent,
                StorageUsagePercent: storagePercent,
                HoneypotsAvailable: honeypotsAvailable,
                StorageAvailableGb: storageAvailable,
                Recommendations: recommendations);
        }

        /// <summary>
        /// Generate simple summary report.
        /// For quick status checks.
        /// </summary>
        public QuotaSummary GenerateSummary(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var status = _violationDetector.GetQuotaStatus(usage, quota);
            var maxPercent = _quotaChecker.GetMaxUsagePercentage(usage, quota);
            var isWithinQuota = _quotaChecker.IsWithinQuota(usage, quota);

            return new QuotaSummary(
                Status: status,
                MaxUsagePercent: maxPercent,
                IsWithinQuota: isWithinQuota);
        }
    }

    /// <summary>
    /// Simple quota summary for quick checks.
    /// </summary>
    public record QuotaSummary(
        QuotaStatus Status,
        decimal MaxUsagePercent,
        bool IsWithinQuota);
}
