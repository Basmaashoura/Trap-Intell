using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Subscriptions.Services
{
    /// <summary>
    /// Domain service for validating subscription quota compliance.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (quota validation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain objects only
    /// ? Encapsulates domain knowledge about quota rules
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic validation)
    /// - Single Responsibility (only quota validation)
    /// - Domain-driven (uses SubscriptionQuota, UsageStatistics)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In SubscriptionQuota.cs:
    ///   public bool IsHoneypotLimitExceeded(int currentHoneypots)
    ///   {
    ///       return currentHoneypots > MaxHoneypots;
    ///   }
    ///   // ?? Simple comparison. Need comprehensive validation!
    /// 
    /// This service provides comprehensive quota validation beyond simple checks.
    /// </summary>
    public class QuotaValidationService
    {
        // Business rule constants
        private const decimal WarningThresholdPercent = 75m;  // 75% = warning
        private const decimal CriticalThresholdPercent = 90m; // 90% = critical
        private const decimal ExceededThresholdPercent = 100m; // 100% = exceeded

        /// <summary>
        /// Validate if usage is within quota limits.
        /// 
        /// BUSINESS RULES:
        /// - Usage must not exceed quota for any resource type
        /// - Both honeypots AND storage must be within limits
        /// 
        /// EXAMPLE:
        /// - Honeypots: 8/10 = ? Within quota
        /// - Storage: 60/50GB = ? Exceeds quota
        /// - Result: NOT within quota
        /// </summary>
        /// <param name="usage">Current usage statistics</param>
        /// <param name="quota">Subscription quota limits</param>
        /// <returns>True if all resources are within quota</returns>
        public bool IsWithinQuota(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            // All resource types must be within quota
            return usage.HoneypotsUsed <= quota.MaxHoneypots &&
                   usage.StorageUsedGb <= quota.MaxStorageGb;
        }

        /// <summary>
        /// Validate quota and get list of violations.
        /// 
        /// BUSINESS RULES:
        /// - Check each resource type independently
        /// - Return detailed violation information
        /// - Include percentage exceeded and excess amount
        /// 
        /// EXAMPLE:
        /// - Honeypots: 12/10 = 120% (2 excess)
        /// - Storage: 60/50GB = 120% (10GB excess)
        /// - Result: 2 violations
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <returns>List of quota violations (empty if within quota)</returns>
        public List<QuotaViolation> ValidateQuota(
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
                var excess = usage.HoneypotsUsed - quota.MaxHoneypots;
                var percentage = quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed);

                violations.Add(new QuotaViolation(
                    QuotaType.Honeypots,
                    CurrentValue: usage.HoneypotsUsed,
                    MaxValue: quota.MaxHoneypots,
                    ExcessAmount: excess,
                    PercentageUsed: percentage,
                    Severity: DetermineSeverity(percentage)));
            }

            // Check storage quota
            if (usage.StorageUsedGb > quota.MaxStorageGb)
            {
                var excess = usage.StorageUsedGb - quota.MaxStorageGb;
                var percentage = quota.GetStorageUsagePercentage(usage.StorageUsedGb);

                violations.Add(new QuotaViolation(
                    QuotaType.Storage,
                    CurrentValue: usage.StorageUsedGb,
                    MaxValue: quota.MaxStorageGb,
                    ExcessAmount: excess,
                    PercentageUsed: percentage,
                    Severity: DetermineSeverity(percentage)));
            }

            return violations;
        }

        /// <summary>
        /// Get quota status based on usage percentage.
        /// 
        /// BUSINESS RULES:
        /// - Normal: < 75%
        /// - Warning: 75-89%
        /// - Critical: 90-99%
        /// - Exceeded: >= 100%
        /// 
        /// EXAMPLE:
        /// - Honeypots: 7/10 = 70% (Normal)
        /// - Storage: 45/50GB = 90% (Critical)
        /// - Result: Critical (worst status wins)
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <returns>Overall quota status</returns>
        public QuotaStatus GetQuotaStatus(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            // Calculate percentages for each resource
            var honeypotPercent = quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed);
            var storagePercent = quota.GetStorageUsagePercentage(usage.StorageUsedGb);

            // Take the worst status (highest percentage)
            var maxPercent = Math.Max(honeypotPercent, storagePercent);

            return DetermineQuotaStatus(maxPercent);
        }

        /// <summary>
        /// Check if a specific resource type is within quota.
        /// Useful for validation before creating new resources.
        /// 
        /// EXAMPLE:
        /// - Before creating honeypot: check if quota allows it
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <param name="resourceType">Type of resource to check</param>
        /// <returns>True if resource is within quota</returns>
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
        /// Check if adding additional usage would exceed quota.
        /// Proactive validation before performing operations.
        /// 
        /// BUSINESS RULE:
        /// - Prevent operations that would exceed quota
        /// - Used before creating honeypots, uploading data, etc.
        /// 
        /// EXAMPLE:
        /// - Current: 9/10 honeypots
        /// - Want to add: 2 honeypots
        /// - Result: Would exceed (9 + 2 = 11 > 10)
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <param name="additionalHoneypots">Additional honeypots to add</param>
        /// <param name="additionalStorageGb">Additional storage to add</param>
        /// <returns>True if additional usage would exceed quota</returns>
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
        /// Calculate how much of a resource can be added before hitting quota.
        /// 
        /// BUSINESS RULE:
        /// - Available = Quota - Current Usage
        /// - Minimum of 0 (no negative availability)
        /// 
        /// EXAMPLE:
        /// - Quota: 10 honeypots
        /// - Current: 7 honeypots
        /// - Available: 3 honeypots
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <param name="resourceType">Type of resource</param>
        /// <returns>Amount available before quota is reached</returns>
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
        /// Get comprehensive quota validation report.
        /// Includes status, violations, warnings, and recommendations.
        /// 
        /// EXAMPLE:
        /// - Status: Warning
        /// - Honeypots: 8/10 (80% - Warning)
        /// - Storage: 45/50GB (90% - Critical)
        /// - Recommendation: Upgrade plan or reduce usage
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <returns>Comprehensive quota report</returns>
        public QuotaValidationReport GenerateReport(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            if (quota == null)
                throw new ArgumentNullException(nameof(quota));

            var status = GetQuotaStatus(usage, quota);
            var violations = ValidateQuota(usage, quota);
            var isWithinQuota = IsWithinQuota(usage, quota);

            var honeypotPercent = quota.GetHoneypotUsagePercentage(usage.HoneypotsUsed);
            var storagePercent = quota.GetStorageUsagePercentage(usage.StorageUsedGb);

            var honeypotAvailable = (int)GetAvailableQuota(usage, quota, QuotaType.Honeypots);
            var storageAvailable = GetAvailableQuota(usage, quota, QuotaType.Storage);

            var recommendations = GenerateRecommendations(status, violations);

            return new QuotaValidationReport(
                Status: status,
                IsWithinQuota: isWithinQuota,
                Violations: violations,
                HoneypotUsagePercent: honeypotPercent,
                StorageUsagePercent: storagePercent,
                HoneypotsAvailable: honeypotAvailable,
                StorageAvailableGb: storageAvailable,
                Recommendations: recommendations);
        }

        /// <summary>
        /// Validate if quota allows specific operation.
        /// Business-specific validation for common operations.
        /// 
        /// BUSINESS RULES:
        /// - Creating honeypot: requires 1 honeypot quota
        /// - Uploading logs: requires storage quota
        /// - Scaling up: requires multiple honeypot quota
        /// </summary>
        /// <param name="usage">Current usage</param>
        /// <param name="quota">Quota limits</param>
        /// <param name="operation">Operation type</param>
        /// <returns>Validation result with details</returns>
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
                    Message: "Unknown operation type")
            };
        }

        #region Private Helper Methods

        /// <summary>
        /// Determine severity based on percentage used.
        /// </summary>
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

        /// <summary>
        /// Determine quota status from percentage.
        /// </summary>
        private QuotaStatus DetermineQuotaStatus(decimal percentage)
        {
            return percentage switch
            {
                >= ExceededThresholdPercent => QuotaStatus.Exceeded,
                >= CriticalThresholdPercent => QuotaStatus.Critical,
                >= WarningThresholdPercent => QuotaStatus.Warning,
                _ => QuotaStatus.Normal
            };
        }

        /// <summary>
        /// Generate recommendations based on quota status.
        /// </summary>
        private List<string> GenerateRecommendations(
            QuotaStatus status,
            List<QuotaViolation> violations)
        {
            var recommendations = new List<string>();

            if (status == QuotaStatus.Exceeded)
            {
                recommendations.Add("?? CRITICAL: Quota exceeded. Upgrade plan immediately or reduce usage.");
                
                foreach (var violation in violations)
                {
                    recommendations.Add($"• Reduce {violation.ResourceType} by {violation.ExcessAmount:F2}");
                }
            }
            else if (status == QuotaStatus.Critical)
            {
                recommendations.Add("?? WARNING: Approaching quota limits (90%+). Plan upgrade recommended.");
                recommendations.Add("• Monitor usage closely");
                recommendations.Add("• Consider upgrading to higher plan");
            }
            else if (status == QuotaStatus.Warning)
            {
                recommendations.Add("?? Usage is at 75%+ of quota. Monitor and plan for growth.");
            }
            else
            {
                recommendations.Add("? Usage is healthy. Continue normal operations.");
            }

            return recommendations;
        }

        /// <summary>
        /// Validate creating a single honeypot.
        /// </summary>
        private QuotaOperationValidation ValidateCreateHoneypot(
            UsageStatistics usage,
            SubscriptionQuota quota)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalHoneypots: 1);

            if (wouldExceed)
            {
                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Cannot create honeypot. Quota: {usage.HoneypotsUsed}/{quota.MaxHoneypots}",
                    RemainingCapacity: 0);
            }

            var remaining = (int)GetAvailableQuota(usage, quota, QuotaType.Honeypots);

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: $"Honeypot creation allowed. {remaining} slots available.",
                RemainingCapacity: remaining);
        }

        /// <summary>
        /// Validate storage operation (upload, etc).
        /// </summary>
        private QuotaOperationValidation ValidateStorageOperation(
            UsageStatistics usage,
            SubscriptionQuota quota,
            decimal requiredStorageGb)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalStorageGb: requiredStorageGb);

            if (wouldExceed)
            {
                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Insufficient storage. Need {requiredStorageGb}GB, available {GetAvailableQuota(usage, quota, QuotaType.Storage):F2}GB",
                    RemainingCapacity: GetAvailableQuota(usage, quota, QuotaType.Storage));
            }

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: "Storage operation allowed",
                RemainingCapacity: GetAvailableQuota(usage, quota, QuotaType.Storage));
        }

        /// <summary>
        /// Validate scaling up operation.
        /// </summary>
        private QuotaOperationValidation ValidateScaleUp(
            UsageStatistics usage,
            SubscriptionQuota quota,
            int additionalHoneypots)
        {
            var wouldExceed = WouldExceedQuota(usage, quota, additionalHoneypots: additionalHoneypots);

            if (wouldExceed)
            {
                var available = (int)GetAvailableQuota(usage, quota, QuotaType.Honeypots);
                
                return new QuotaOperationValidation(
                    IsAllowed: false,
                    Message: $"Cannot scale up by {additionalHoneypots}. Only {available} slots available.",
                    RemainingCapacity: available);
            }

            return new QuotaOperationValidation(
                IsAllowed: true,
                Message: $"Scale up by {additionalHoneypots} allowed",
                RemainingCapacity: (int)GetAvailableQuota(usage, quota, QuotaType.Honeypots));
        }

        #endregion
    }

    #region Supporting Value Objects and Enums

    /// <summary>
    /// Types of quota resources.
    /// </summary>
    public enum QuotaType
    {
        Honeypots,
        Storage,
        ApiCalls,
        Users
    }

    /// <summary>
    /// Quota status levels.
    /// </summary>
    public enum QuotaStatus
    {
        Normal = 0,      // < 75%
        Warning = 1,     // 75-89%
        Critical = 2,    // 90-99%
        Exceeded = 3     // >= 100%
    }

    /// <summary>
    /// Quota violation severity.
    /// </summary>
    public enum QuotaSeverity
    {
        Normal,
        Warning,
        Critical,
        Exceeded
    }

    /// <summary>
    /// Types of quota operations.
    /// </summary>
    public enum QuotaOperation
    {
        CreateHoneypot,
        UploadLogs,
        ScaleUp,
        AddUser
    }

    /// <summary>
    /// Represents a quota violation.
    /// </summary>
    public record QuotaViolation(
        QuotaType ResourceType,
        decimal CurrentValue,
        decimal MaxValue,
        decimal ExcessAmount,
        decimal PercentageUsed,
        QuotaSeverity Severity);

    /// <summary>
    /// Comprehensive quota validation report.
    /// </summary>
    public record QuotaValidationReport(
        QuotaStatus Status,
        bool IsWithinQuota,
        List<QuotaViolation> Violations,
        decimal HoneypotUsagePercent,
        decimal StorageUsagePercent,
        int HoneypotsAvailable,
        decimal StorageAvailableGb,
        List<string> Recommendations);

    /// <summary>
    /// Validation result for specific operations.
    /// </summary>
    public record QuotaOperationValidation(
        bool IsAllowed,
        string Message,
        decimal RemainingCapacity = 0);

    #endregion
}
