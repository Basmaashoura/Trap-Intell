using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Configuration record for quota limits.
    /// Used for passing quota data between layers.
    /// For persistence, use SubscriptionQuotaEntity instead.
    /// </summary>
    /// <remarks>
    /// This is a configuration/transfer record, not an entity.
    /// The actual quota tracking is done by SubscriptionQuotaEntity
    /// which is owned by the Subscription aggregate.
    /// </remarks>
    public record SubscriptionQuota
    {
        public int MaxHoneypots { get; }
        public decimal MaxStorageGb { get; }
        public decimal MaxMonthlyApiCalls { get; }
        public decimal MaxConcurrentUsers { get; }
        public bool HardLimitEnforced { get; }

        public SubscriptionQuota(
            int maxHoneypots,
            decimal maxStorageGb,
            decimal maxMonthlyApiCalls = 1000000,
            decimal maxConcurrentUsers = 100,
            bool hardLimitEnforced = false)
        {
            MaxHoneypots = maxHoneypots >= 0 ? maxHoneypots : 0;
            MaxStorageGb = maxStorageGb >= 0 ? maxStorageGb : 0;
            MaxMonthlyApiCalls = maxMonthlyApiCalls >= 0 ? maxMonthlyApiCalls : 0;
            MaxConcurrentUsers = maxConcurrentUsers >= 0 ? maxConcurrentUsers : 0;
            HardLimitEnforced = hardLimitEnforced;
        }

        /// <summary>
        /// Create from Result pattern for validation.
        /// </summary>
        public static Result<SubscriptionQuota> Create(
            int maxHoneypots,
            decimal maxStorageGb,
            decimal maxMonthlyApiCalls = 1000000,
            decimal maxConcurrentUsers = 100,
            bool hardLimitEnforced = false)
        {
            if (maxHoneypots < 0)
                return Result.Failure<SubscriptionQuota>(QuotaErrors.InvalidMaxHoneypots);

            if (maxStorageGb < 0)
                return Result.Failure<SubscriptionQuota>(QuotaErrors.InvalidMaxStorage);

            if (maxMonthlyApiCalls < 0)
                return Result.Failure<SubscriptionQuota>(QuotaErrors.InvalidMaxApiCalls);

            if (maxConcurrentUsers < 0)
                return Result.Failure<SubscriptionQuota>(QuotaErrors.InvalidMaxUsers);

            return Result.Success(new SubscriptionQuota(
                maxHoneypots,
                maxStorageGb,
                maxMonthlyApiCalls,
                maxConcurrentUsers,
                hardLimitEnforced));
        }

        /// <summary>
        /// Check if honeypot usage exceeds quota.
        /// </summary>
        public bool IsHoneypotLimitExceeded(int currentHoneypots)
            => currentHoneypots > MaxHoneypots;

        /// <summary>
        /// Check if storage usage exceeds quota.
        /// </summary>
        public bool IsStorageLimitExceeded(decimal currentStorageGb)
            => currentStorageGb > MaxStorageGb;

        /// <summary>
        /// Calculate percentage of quota used for honeypots.
        /// </summary>
        public decimal GetHoneypotUsagePercentage(int currentHoneypots)
            => MaxHoneypots > 0 ? (decimal)currentHoneypots / MaxHoneypots * 100 : 0;

        /// <summary>
        /// Calculate percentage of quota used for storage.
        /// </summary>
        public decimal GetStorageUsagePercentage(decimal currentStorageGb)
            => MaxStorageGb > 0 ? currentStorageGb / MaxStorageGb * 100 : 0;

        /// <summary>
        /// Get remaining honeypots available.
        /// </summary>
        public int GetRemainingHoneypots(int currentHoneypots)
            => Math.Max(0, MaxHoneypots - currentHoneypots);

        /// <summary>
        /// Get remaining storage available.
        /// </summary>
        public decimal GetRemainingStorage(decimal currentStorageGb)
            => Math.Max(0, MaxStorageGb - currentStorageGb);
    }

    /// <summary>
    /// Tracks monthly usage metrics and alerts.
    /// </summary>
    public record MonthlyUsage
    {
        public int CurrentMonth { get; }
        public int CurrentYear { get; }
        public int ApiCallsUsed { get; }
        public DateTime TrackingStartDate { get; }
        public DateTime TrackingEndDate { get; }

        public MonthlyUsage(
            int apiCallsUsed,
            int month = -1,
            int year = -1)
        {
            CurrentMonth = month > 0 ? month : DateTime.UtcNow.Month;
            CurrentYear = year > 0 ? year : DateTime.UtcNow.Year;
            ApiCallsUsed = apiCallsUsed >= 0 ? apiCallsUsed : 0;

            TrackingStartDate = new DateTime(CurrentYear, CurrentMonth, 1);
            TrackingEndDate = TrackingStartDate.AddMonths(1).AddDays(-1);
        }

        public bool IsCurrentMonth()
        {
            var now = DateTime.UtcNow;
            return CurrentMonth == now.Month && CurrentYear == now.Year;
        }

        public int GetDaysRemainingInMonth()
        {
            var now = DateTime.UtcNow;
            if (!IsCurrentMonth())
                return 0;

            return (int)(TrackingEndDate - now).TotalDays + 1;
        }

        public decimal GetMonthElapsedPercentage()
        {
            var daysInMonth = (int)(TrackingEndDate - TrackingStartDate).TotalDays + 1;
            var daysElapsed = (int)(DateTime.UtcNow - TrackingStartDate).TotalDays;
            return daysInMonth > 0 ? (decimal)daysElapsed / daysInMonth * 100 : 0;
        }
    }

    /// <summary>
    /// Usage alert thresholds and notifications.
    /// </summary>
    public record UsageAlert
    {
        public UsageAlertType AlertType { get; }
        public decimal ThresholdPercentage { get; }
        public bool IsEnabled { get; }
        public DateTime? TriggeredAt { get; }
        public string Message { get; }

        public UsageAlert(
            UsageAlertType alertType,
            decimal thresholdPercentage = 80,
            bool isEnabled = true,
            DateTime? triggeredAt = null,
            string? customMessage = null)
        {
            AlertType = alertType;
            ThresholdPercentage = Math.Clamp(thresholdPercentage, 0, 100);
            IsEnabled = isEnabled;
            TriggeredAt = triggeredAt;
            Message = customMessage ?? GetDefaultMessage(alertType, thresholdPercentage);
        }

        private static string GetDefaultMessage(UsageAlertType type, decimal threshold)
        {
            return type switch
            {
                UsageAlertType.StorageWarning => $"Storage usage has reached {threshold}% of quota",
                UsageAlertType.HoneypotWarning => $"Honeypot usage has reached {threshold}% of quota",
                UsageAlertType.ApiCallsWarning => $"API calls usage has reached {threshold}% of monthly limit",
                UsageAlertType.StorageExceeded => "Storage quota exceeded. Hard limit is enforced.",
                UsageAlertType.HoneypotExceeded => "Honeypot quota exceeded. No new honeypots can be deployed.",
                UsageAlertType.ApiCallsExceeded => "Monthly API call limit exceeded.",
                _ => "Usage limit alert"
            };
        }

        public bool ShouldTrigger(decimal currentUsagePercentage)
            => IsEnabled && currentUsagePercentage >= ThresholdPercentage && TriggeredAt is null;
    }

    /// <summary>
    /// Usage alert types.
    /// </summary>
    public enum UsageAlertType
    {
        StorageWarning = 0,
        HoneypotWarning = 1,
        ApiCallsWarning = 2,
        StorageExceeded = 3,
        HoneypotExceeded = 4,
        ApiCallsExceeded = 5
    }

    /// <summary>
    /// Usage enforcement action.
    /// </summary>
    public enum UsageEnforcementAction
    {
        Allow = 0,
        Warn = 1,
        RateLimited = 2,
        Denied = 3
    }

    /// <summary>
    /// Represents the result of a usage validation.
    /// </summary>
    public record UsageValidationResult
    {
        public UsageEnforcementAction Action { get; }
        public decimal CurrentPercentage { get; }
        public decimal RemainingQuota { get; }
        public string Message { get; }
        public UsageAlert? TriggeredAlert { get; }

        public UsageValidationResult(
            UsageEnforcementAction action,
            decimal currentPercentage,
            decimal remainingQuota,
            string message,
            UsageAlert? triggeredAlert = null)
        {
            Action = action;
            CurrentPercentage = currentPercentage;
            RemainingQuota = remainingQuota;
            Message = message;
            TriggeredAlert = triggeredAlert;
        }

        public bool IsAllowed => Action == UsageEnforcementAction.Allow || Action == UsageEnforcementAction.Warn;
    }
}
