using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Subscriptions.Entities;

/// <summary>
/// Represents aggregated usage for a billing month.
/// Used for invoice generation and billing calculations.
/// Owned entity managed by Subscription aggregate.
/// </summary>
public class MonthlyUsageSummary : Entity<Guid>
{
    // Private constructor for EF
    private MonthlyUsageSummary() { }

    private MonthlyUsageSummary(
        Guid id,
        Guid subscriptionId,
        int year,
        int month)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        Year = year;
        Month = month;
        PeriodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        PeriodEnd = PeriodStart.AddMonths(1).AddTicks(-1);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #region Properties

    /// <summary>
    /// Parent subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>
    /// Year of this summary.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Month of this summary (1-12).
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Start of billing period.
    /// </summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>
    /// End of billing period.
    /// </summary>
    public DateTime PeriodEnd { get; private set; }

    /// <summary>
    /// Peak number of honeypots during this month.
    /// </summary>
    public int PeakHoneypots { get; private set; }

    /// <summary>
    /// Peak storage usage in GB during this month.
    /// </summary>
    public decimal PeakStorageGb { get; private set; }

    /// <summary>
    /// Total API calls made during this month.
    /// </summary>
    public int TotalApiCalls { get; private set; }

    /// <summary>
    /// Average daily honeypots.
    /// </summary>
    public decimal AverageHoneypots { get; private set; }

    /// <summary>
    /// Average daily storage in GB.
    /// </summary>
    public decimal AverageStorageGb { get; private set; }

    /// <summary>
    /// Total events captured during this month.
    /// </summary>
    public int TotalEventsCaptured { get; private set; }

    /// <summary>
    /// Calculated overage charges for this month.
    /// </summary>
    public decimal OverageCharges { get; private set; }

    /// <summary>
    /// Whether this summary has been billed.
    /// </summary>
    public bool IsBilled { get; private set; }

    /// <summary>
    /// Invoice ID if billed.
    /// </summary>
    public Guid? InvoiceId { get; private set; }

    /// <summary>
    /// When this summary was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; private set; }

    /// <summary>
    /// Whether this summary is finalized (month has ended).
    /// </summary>
    public bool IsFinalized { get; private set; }

    /// <summary>
    /// When summary was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When summary was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create new monthly usage summary.
    /// </summary>
    public static Result<MonthlyUsageSummary> Create(
        Guid subscriptionId,
        int year,
        int month)
    {
        if (subscriptionId == Guid.Empty)
            return Result.Failure<MonthlyUsageSummary>(QuotaErrors.InvalidSubscriptionId);

        if (year < 2020 || year > 2100)
            return Result.Failure<MonthlyUsageSummary>(QuotaErrors.InvalidYear);

        if (month < 1 || month > 12)
            return Result.Failure<MonthlyUsageSummary>(QuotaErrors.InvalidMonth);

        var summary = new MonthlyUsageSummary(
            Guid.NewGuid(),
            subscriptionId,
            year,
            month);

        return Result.Success(summary);
    }

    /// <summary>
    /// Create for current month.
    /// </summary>
    public static Result<MonthlyUsageSummary> CreateForCurrentMonth(Guid subscriptionId)
    {
        var now = DateTime.UtcNow;
        return Create(subscriptionId, now.Year, now.Month);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static MonthlyUsageSummary Reconstruct(
        Guid id,
        Guid subscriptionId,
        int year,
        int month,
        DateTime periodStart,
        DateTime periodEnd,
        int peakHoneypots,
        decimal peakStorageGb,
        int totalApiCalls,
        decimal averageHoneypots,
        decimal averageStorageGb,
        int totalEventsCaptured,
        decimal overageCharges,
        bool isBilled,
        Guid? invoiceId,
        DateTime? finalizedAt,
        bool isFinalized,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new MonthlyUsageSummary
        {
            Id = id,
            SubscriptionId = subscriptionId,
            Year = year,
            Month = month,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeakHoneypots = peakHoneypots,
            PeakStorageGb = peakStorageGb,
            TotalApiCalls = totalApiCalls,
            AverageHoneypots = averageHoneypots,
            AverageStorageGb = averageStorageGb,
            TotalEventsCaptured = totalEventsCaptured,
            OverageCharges = overageCharges,
            IsBilled = isBilled,
            InvoiceId = invoiceId,
            FinalizedAt = finalizedAt,
            IsFinalized = isFinalized,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Update usage metrics from a snapshot.
    /// </summary>
    public Result UpdateFromSnapshot(UsageSnapshot snapshot)
    {
        if (IsFinalized)
            return Result.Failure(QuotaErrors.SummaryAlreadyFinalized);

        // Update peaks
        if (snapshot.HoneypotsActive > PeakHoneypots)
            PeakHoneypots = snapshot.HoneypotsActive;

        if (snapshot.StorageUsedGb > PeakStorageGb)
            PeakStorageGb = snapshot.StorageUsedGb;

        // Accumulate API calls
        TotalApiCalls += snapshot.ApiCallsCount;
        TotalEventsCaptured += snapshot.EventsCaptured;

        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Calculate and set overage charges.
    /// </summary>
    public Result CalculateOverages(SubscriptionQuotaEntity quota)
    {
        if (IsFinalized)
            return Result.Failure(QuotaErrors.SummaryAlreadyFinalized);

        OverageCharges = quota.CalculateOverageCharges(PeakHoneypots, PeakStorageGb);
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Finalize averages from snapshots.
    /// </summary>
    public Result FinalizeAverages(IReadOnlyList<UsageSnapshot> monthSnapshots)
    {
        if (IsFinalized)
            return Result.Failure(QuotaErrors.SummaryAlreadyFinalized);

        if (monthSnapshots.Count > 0)
        {
            AverageHoneypots = (decimal)monthSnapshots.Average(s => s.HoneypotsActive);
            AverageStorageGb = monthSnapshots.Average(s => s.StorageUsedGb);
        }

        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Finalize the monthly summary (end of month).
    /// </summary>
    public Result Finalize()
    {
        if (IsFinalized)
            return Result.Success(); // Already finalized

        // Can only finalize past months
        var now = DateTime.UtcNow;
        if (Year > now.Year || (Year == now.Year && Month >= now.Month))
            return Result.Failure(QuotaErrors.CannotFinalizeCurrentMonth);

        IsFinalized = true;
        FinalizedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Mark as billed with invoice reference.
    /// </summary>
    public Result MarkAsBilled(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            return Result.Failure(QuotaErrors.InvalidInvoiceId);

        if (IsBilled)
            return Result.Failure(QuotaErrors.AlreadyBilled);

        IsBilled = true;
        InvoiceId = invoiceId;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if this is the current month.
    /// </summary>
    public bool IsCurrentMonth()
    {
        var now = DateTime.UtcNow;
        return Year == now.Year && Month == now.Month;
    }

    /// <summary>
    /// Get days remaining in this period.
    /// </summary>
    public int GetDaysRemaining()
    {
        if (!IsCurrentMonth())
            return 0;

        return Math.Max(0, (int)(PeriodEnd - DateTime.UtcNow).TotalDays);
    }

    /// <summary>
    /// Get period display string (e.g., "January 2024").
    /// </summary>
    public string GetPeriodDisplay()
    {
        return new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    /// <summary>
    /// Check if has any overage charges.
    /// </summary>
    public bool HasOverages() => OverageCharges > 0;

    #endregion
}
