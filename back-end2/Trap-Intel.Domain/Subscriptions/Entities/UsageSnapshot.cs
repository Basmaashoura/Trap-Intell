using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Subscriptions.Entities;

/// <summary>
/// Represents a point-in-time snapshot of subscription usage.
/// Used for historical tracking, trend analysis, and billing.
/// Owned entity managed by Subscription aggregate.
/// </summary>
public class UsageSnapshot : Entity<Guid>
{
    // Private constructor for EF
    private UsageSnapshot() { }

    private UsageSnapshot(
        Guid id,
        Guid subscriptionId,
        UsagePeriodType periodType,
        int honeypotsActive,
        decimal storageUsedGb,
        int apiCallsCount,
        int activeUsers,
        int eventsCaptured)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        PeriodType = periodType;
        RecordedAt = DateTime.UtcNow;
        HoneypotsActive = honeypotsActive;
        StorageUsedGb = storageUsedGb;
        ApiCallsCount = apiCallsCount;
        ActiveUsers = activeUsers;
        EventsCaptured = eventsCaptured;
    }

    #region Properties

    /// <summary>
    /// Parent subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>
    /// When this snapshot was recorded.
    /// </summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>
    /// Type of period this snapshot represents.
    /// </summary>
    public UsagePeriodType PeriodType { get; private set; }

    /// <summary>
    /// Number of active honeypots at snapshot time.
    /// </summary>
    public int HoneypotsActive { get; private set; }

    /// <summary>
    /// Storage used in GB at snapshot time.
    /// </summary>
    public decimal StorageUsedGb { get; private set; }

    /// <summary>
    /// API calls made in this period.
    /// </summary>
    public int ApiCallsCount { get; private set; }

    /// <summary>
    /// Number of active users at snapshot time.
    /// </summary>
    public int ActiveUsers { get; private set; }

    /// <summary>
    /// Total events captured in this period.
    /// </summary>
    public int EventsCaptured { get; private set; }

    /// <summary>
    /// Change in storage since last snapshot (calculated).
    /// </summary>
    public decimal? StorageDeltaGb { get; private set; }

    /// <summary>
    /// Change in honeypots since last snapshot (calculated).
    /// </summary>
    public int? HoneypotsDelta { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create new usage snapshot.
    /// </summary>
    public static Result<UsageSnapshot> Create(
        Guid subscriptionId,
        UsagePeriodType periodType,
        int honeypotsActive,
        decimal storageUsedGb,
        int apiCallsCount = 0,
        int activeUsers = 0,
        int eventsCaptured = 0)
    {
        if (subscriptionId == Guid.Empty)
            return Result.Failure<UsageSnapshot>(QuotaErrors.InvalidSubscriptionId);

        if (honeypotsActive < 0)
            return Result.Failure<UsageSnapshot>(QuotaErrors.InvalidHoneypotCount);

        if (storageUsedGb < 0)
            return Result.Failure<UsageSnapshot>(QuotaErrors.InvalidStorageValue);

        if (apiCallsCount < 0)
            return Result.Failure<UsageSnapshot>(QuotaErrors.InvalidApiCallCount);

        var snapshot = new UsageSnapshot(
            Guid.NewGuid(),
            subscriptionId,
            periodType,
            honeypotsActive,
            storageUsedGb,
            apiCallsCount,
            activeUsers,
            eventsCaptured);

        return Result.Success(snapshot);
    }

    /// <summary>
    /// Create snapshot with delta calculations from previous snapshot.
    /// </summary>
    public static Result<UsageSnapshot> CreateWithDelta(
        Guid subscriptionId,
        UsagePeriodType periodType,
        int honeypotsActive,
        decimal storageUsedGb,
        int apiCallsCount,
        int activeUsers,
        int eventsCaptured,
        UsageSnapshot? previousSnapshot)
    {
        var result = Create(subscriptionId, periodType, honeypotsActive, storageUsedGb, apiCallsCount, activeUsers, eventsCaptured);
        
        if (result.IsFailure)
            return result;

        var snapshot = result.Value;

        if (previousSnapshot != null)
        {
            snapshot.StorageDeltaGb = storageUsedGb - previousSnapshot.StorageUsedGb;
            snapshot.HoneypotsDelta = honeypotsActive - previousSnapshot.HoneypotsActive;
        }

        return Result.Success(snapshot);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static UsageSnapshot Reconstruct(
        Guid id,
        Guid subscriptionId,
        DateTime recordedAt,
        UsagePeriodType periodType,
        int honeypotsActive,
        decimal storageUsedGb,
        int apiCallsCount,
        int activeUsers,
        int eventsCaptured,
        decimal? storageDeltaGb,
        int? honeypotsDelta)
    {
        return new UsageSnapshot
        {
            Id = id,
            SubscriptionId = subscriptionId,
            RecordedAt = recordedAt,
            PeriodType = periodType,
            HoneypotsActive = honeypotsActive,
            StorageUsedGb = storageUsedGb,
            ApiCallsCount = apiCallsCount,
            ActiveUsers = activeUsers,
            EventsCaptured = eventsCaptured,
            StorageDeltaGb = storageDeltaGb,
            HoneypotsDelta = honeypotsDelta
        };
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if this snapshot is from today.
    /// </summary>
    public bool IsToday() => RecordedAt.Date == DateTime.UtcNow.Date;

    /// <summary>
    /// Check if this snapshot is from current month.
    /// </summary>
    public bool IsCurrentMonth() => 
        RecordedAt.Year == DateTime.UtcNow.Year && 
        RecordedAt.Month == DateTime.UtcNow.Month;

    /// <summary>
    /// Get age of this snapshot.
    /// </summary>
    public TimeSpan GetAge() => DateTime.UtcNow - RecordedAt;

    #endregion
}

/// <summary>
/// Type of usage period for snapshots.
/// </summary>
public enum UsagePeriodType
{
    /// <summary>Hourly snapshot (for high-frequency tracking).</summary>
    Hourly = 0,
    
    /// <summary>Daily snapshot (standard).</summary>
    Daily = 1,
    
    /// <summary>Weekly snapshot.</summary>
    Weekly = 2,
    
    /// <summary>Monthly snapshot (for billing).</summary>
    Monthly = 3,
    
    /// <summary>On-demand snapshot (triggered by specific event).</summary>
    OnDemand = 4
}
