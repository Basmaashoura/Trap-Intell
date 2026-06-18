using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Subscriptions.Entities;

/// <summary>
/// Represents the quota limits for a subscription.
/// Owned entity managed by Subscription aggregate.
/// Tracks quota history when plan changes occur.
/// </summary>
public class SubscriptionQuotaEntity : Entity<Guid>
{
    // Private constructor for EF
    private SubscriptionQuotaEntity() { }

    private SubscriptionQuotaEntity(
        Guid id,
        Guid subscriptionId,
        int maxHoneypots,
        decimal maxStorageGb,
        int maxMonthlyApiCalls,
        int maxUsers,
        bool hardLimitEnforced,
        decimal overageHoneypotRate,
        decimal overageStorageRatePerGb,
        Guid? sourcePlanId)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        MaxHoneypots = maxHoneypots;
        MaxStorageGb = maxStorageGb;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        MaxUsers = maxUsers;
        HardLimitEnforced = hardLimitEnforced;
        OverageHoneypotRate = overageHoneypotRate;
        OverageStorageRatePerGb = overageStorageRatePerGb;
        SourcePlanId = sourcePlanId;
        EffectiveFrom = DateTime.UtcNow;
        EffectiveTo = null;
        IsActive = true;
    }

    #region Properties

    /// <summary>
    /// Parent subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>
    /// Maximum number of honeypots allowed.
    /// </summary>
    public int MaxHoneypots { get; private set; }

    /// <summary>
    /// Maximum storage in GB allowed.
    /// </summary>
    public decimal MaxStorageGb { get; private set; }

    /// <summary>
    /// Maximum API calls per month.
    /// </summary>
    public int MaxMonthlyApiCalls { get; private set; }

    /// <summary>
    /// Maximum concurrent users.
    /// </summary>
    public int MaxUsers { get; private set; }

    /// <summary>
    /// Whether hard limits are enforced (block vs. overage charge).
    /// </summary>
    public bool HardLimitEnforced { get; private set; }

    /// <summary>
    /// Overage charge per additional honeypot.
    /// </summary>
    public decimal OverageHoneypotRate { get; private set; }

    /// <summary>
    /// Overage charge per additional GB of storage.
    /// </summary>
    public decimal OverageStorageRatePerGb { get; private set; }

    /// <summary>
    /// Plan that this quota was derived from (for audit).
    /// </summary>
    public Guid? SourcePlanId { get; private set; }

    /// <summary>
    /// When this quota became effective.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// When this quota was superseded (null if current).
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Whether this is the current active quota.
    /// </summary>
    public bool IsActive { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create new quota entity from plan limits.
    /// </summary>
    public static Result<SubscriptionQuotaEntity> Create(
        Guid subscriptionId,
        int maxHoneypots,
        decimal maxStorageGb,
        int maxMonthlyApiCalls = 1000000,
        int maxUsers = 100,
        bool hardLimitEnforced = false,
        decimal overageHoneypotRate = 10m,
        decimal overageStorageRatePerGb = 0.50m,
        Guid? sourcePlanId = null)
    {
        if (subscriptionId == Guid.Empty)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidSubscriptionId);

        if (maxHoneypots < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidMaxHoneypots);

        if (maxStorageGb < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidMaxStorage);

        if (maxMonthlyApiCalls < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidMaxApiCalls);

        if (maxUsers < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidMaxUsers);

        if (overageHoneypotRate < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidOverageRate);

        if (overageStorageRatePerGb < 0)
            return Result.Failure<SubscriptionQuotaEntity>(QuotaErrors.InvalidOverageRate);

        var entity = new SubscriptionQuotaEntity(
            Guid.NewGuid(),
            subscriptionId,
            maxHoneypots,
            maxStorageGb,
            maxMonthlyApiCalls,
            maxUsers,
            hardLimitEnforced,
            overageHoneypotRate,
            overageStorageRatePerGb,
            sourcePlanId);

        return Result.Success(entity);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static SubscriptionQuotaEntity Reconstruct(
        Guid id,
        Guid subscriptionId,
        int maxHoneypots,
        decimal maxStorageGb,
        int maxMonthlyApiCalls,
        int maxUsers,
        bool hardLimitEnforced,
        decimal overageHoneypotRate,
        decimal overageStorageRatePerGb,
        Guid? sourcePlanId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        bool isActive)
    {
        return new SubscriptionQuotaEntity
        {
            Id = id,
            SubscriptionId = subscriptionId,
            MaxHoneypots = maxHoneypots,
            MaxStorageGb = maxStorageGb,
            MaxMonthlyApiCalls = maxMonthlyApiCalls,
            MaxUsers = maxUsers,
            HardLimitEnforced = hardLimitEnforced,
            OverageHoneypotRate = overageHoneypotRate,
            OverageStorageRatePerGb = overageStorageRatePerGb,
            SourcePlanId = sourcePlanId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = isActive
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Close this quota's effective period (when superseded by new quota).
    /// </summary>
    internal void CloseEffectivePeriod()
    {
        EffectiveTo = DateTime.UtcNow;
        IsActive = false;
    }

    /// <summary>
    /// Check if honeypot limit is exceeded.
    /// </summary>
    public bool IsHoneypotLimitExceeded(int currentHoneypots)
        => currentHoneypots > MaxHoneypots;

    /// <summary>
    /// Check if storage limit is exceeded.
    /// </summary>
    public bool IsStorageLimitExceeded(decimal currentStorageGb)
        => currentStorageGb > MaxStorageGb;

    /// <summary>
    /// Check if API call limit is exceeded.
    /// </summary>
    public bool IsApiCallLimitExceeded(int currentApiCalls)
        => currentApiCalls > MaxMonthlyApiCalls;

    /// <summary>
    /// Get honeypot usage percentage.
    /// </summary>
    public decimal GetHoneypotUsagePercent(int currentHoneypots)
        => MaxHoneypots > 0 ? (decimal)currentHoneypots / MaxHoneypots * 100 : 0;

    /// <summary>
    /// Get storage usage percentage.
    /// </summary>
    public decimal GetStorageUsagePercent(decimal currentStorageGb)
        => MaxStorageGb > 0 ? currentStorageGb / MaxStorageGb * 100 : 0;

    /// <summary>
    /// Get API calls usage percentage.
    /// </summary>
    public decimal GetApiCallsUsagePercent(int currentApiCalls)
        => MaxMonthlyApiCalls > 0 ? (decimal)currentApiCalls / MaxMonthlyApiCalls * 100 : 0;

    /// <summary>
    /// Calculate overage charges for given usage.
    /// </summary>
    public decimal CalculateOverageCharges(int currentHoneypots, decimal currentStorageGb)
    {
        var honeypotOverage = Math.Max(0, currentHoneypots - MaxHoneypots);
        var storageOverage = Math.Max(0, currentStorageGb - MaxStorageGb);

        return (honeypotOverage * OverageHoneypotRate) + (storageOverage * OverageStorageRatePerGb);
    }

    /// <summary>
    /// Get remaining honeypots available.
    /// </summary>
    public int GetRemainingHoneypots(int currentHoneypots)
        => Math.Max(0, MaxHoneypots - currentHoneypots);

    /// <summary>
    /// Get remaining storage available.
    /// </summary>
    public decimal GetRemainingStorageGb(decimal currentStorageGb)
        => Math.Max(0, MaxStorageGb - currentStorageGb);

    #endregion
}
