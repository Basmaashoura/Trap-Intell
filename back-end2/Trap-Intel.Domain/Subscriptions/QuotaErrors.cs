using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Subscriptions;

/// <summary>
/// Error definitions for Quota and Usage operations.
/// </summary>
public static class QuotaErrors
{
    // Validation Errors
    public static readonly Error InvalidSubscriptionId = Error.Custom(
        "Quota.InvalidSubscriptionId",
        "Subscription ID cannot be empty.");

    public static readonly Error InvalidMaxHoneypots = Error.Custom(
        "Quota.InvalidMaxHoneypots",
        "Maximum honeypots cannot be negative.");

    public static readonly Error InvalidMaxStorage = Error.Custom(
        "Quota.InvalidMaxStorage",
        "Maximum storage cannot be negative.");

    public static readonly Error InvalidMaxApiCalls = Error.Custom(
        "Quota.InvalidMaxApiCalls",
        "Maximum API calls cannot be negative.");

    public static readonly Error InvalidMaxUsers = Error.Custom(
        "Quota.InvalidMaxUsers",
        "Maximum users cannot be negative.");

    public static readonly Error InvalidOverageRate = Error.Custom(
        "Quota.InvalidOverageRate",
        "Overage rate cannot be negative.");

    public static readonly Error InvalidHoneypotCount = Error.Custom(
        "Quota.InvalidHoneypotCount",
        "Honeypot count cannot be negative.");

    public static readonly Error InvalidStorageValue = Error.Custom(
        "Quota.InvalidStorageValue",
        "Storage value cannot be negative.");

    public static readonly Error InvalidApiCallCount = Error.Custom(
        "Quota.InvalidApiCallCount",
        "API call count cannot be negative.");

    public static readonly Error InvalidYear = Error.Custom(
        "Quota.InvalidYear",
        "Year must be between 2020 and 2100.");

    public static readonly Error InvalidMonth = Error.Custom(
        "Quota.InvalidMonth",
        "Month must be between 1 and 12.");

    public static readonly Error InvalidInvoiceId = Error.Custom(
        "Quota.InvalidInvoiceId",
        "Invoice ID cannot be empty.");

    // Limit Exceeded Errors
    public static readonly Error HoneypotLimitExceeded = Error.Custom(
        "Quota.HoneypotLimitExceeded",
        "Honeypot limit has been exceeded. Upgrade your plan or remove existing honeypots.");

    public static readonly Error StorageLimitExceeded = Error.Custom(
        "Quota.StorageLimitExceeded",
        "Storage limit has been exceeded. Upgrade your plan or free up storage.");

    public static readonly Error ApiCallLimitExceeded = Error.Custom(
        "Quota.ApiCallLimitExceeded",
        "Monthly API call limit has been exceeded.");

    public static readonly Error UserLimitExceeded = Error.Custom(
        "Quota.UserLimitExceeded",
        "User limit has been exceeded. Upgrade your plan.");

    public static readonly Error HardLimitEnforced = Error.Custom(
        "Quota.HardLimitEnforced",
        "Operation blocked: hard limit is enforced and quota is exceeded.");

    // State Errors
    public static readonly Error SummaryAlreadyFinalized = Error.Custom(
        "Quota.SummaryAlreadyFinalized",
        "Monthly summary is already finalized and cannot be modified.");

    public static readonly Error CannotFinalizeCurrentMonth = Error.Custom(
        "Quota.CannotFinalizeCurrentMonth",
        "Cannot finalize the current month's summary.");

    public static readonly Error AlreadyBilled = Error.Custom(
        "Quota.AlreadyBilled",
        "This usage period has already been billed.");

    public static readonly Error NoActiveQuota = Error.Custom(
        "Quota.NoActiveQuota",
        "Subscription does not have an active quota configuration.");

    public static readonly Error QuotaNotFound = Error.Custom(
        "Quota.NotFound",
        "Quota configuration not found.");

    // Usage Errors
    public static readonly Error SnapshotNotFound = Error.Custom(
        "Quota.SnapshotNotFound",
        "Usage snapshot not found.");

    public static readonly Error SummaryNotFound = Error.Custom(
        "Quota.SummaryNotFound",
        "Monthly usage summary not found.");

    public static readonly Error InsufficientHistoryForTrend = Error.Custom(
        "Quota.InsufficientHistoryForTrend",
        "Insufficient usage history for trend analysis. Need at least 2 data points.");

    // Warning Thresholds
    public static readonly Error ApproachingHoneypotLimit = Error.Custom(
        "Quota.ApproachingHoneypotLimit",
        "Approaching honeypot limit (80% used).");

    public static readonly Error ApproachingStorageLimit = Error.Custom(
        "Quota.ApproachingStorageLimit",
        "Approaching storage limit (80% used).");

    public static readonly Error ApproachingApiCallLimit = Error.Custom(
        "Quota.ApproachingApiCallLimit",
        "Approaching monthly API call limit (80% used).");

    // Factory methods for dynamic errors
    public static Error HoneypotLimitExceededDetail(int current, int max) => Error.Custom(
        "Quota.HoneypotLimitExceeded",
        $"Honeypot limit exceeded: {current}/{max} honeypots used.");

    public static Error StorageLimitExceededDetail(decimal current, decimal max) => Error.Custom(
        "Quota.StorageLimitExceeded",
        $"Storage limit exceeded: {current:F2}/{max:F2} GB used.");

    public static Error ApiCallLimitExceededDetail(int current, int max) => Error.Custom(
        "Quota.ApiCallLimitExceeded",
        $"API call limit exceeded: {current}/{max} calls this month.");

    public static Error UsagePercentageWarning(string resource, decimal percentage) => Error.Custom(
        $"Quota.{resource}Warning",
        $"{resource} usage at {percentage:F1}% of quota.");
}
