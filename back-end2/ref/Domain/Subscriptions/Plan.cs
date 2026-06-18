using SharedKernel;

namespace Domain.Subscriptions;

/// <summary>
/// Represents a subscription plan (e.g., Free, Basic, Pro).
/// </summary>
public sealed class Plan : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Plan display name (e.g., "Free", "Basic", "Pro").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Short description of what the plan includes.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Monthly price in the smallest currency unit.
    /// </summary>
    public decimal MonthlyPrice { get; private set; }

    /// <summary>
    /// Yearly price in the smallest currency unit (usually discounted).
    /// </summary>
    public decimal YearlyPrice { get; private set; }

    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EGP").
    /// </summary>
    public string CurrencyCode { get; private set; } = "USD";

    /// <summary>
    /// Maximum number of monitored users allowed on this plan.
    /// </summary>
    public int MaxUsers { get; private set; }

    /// <summary>
    /// Maximum days of stress data history retained.
    /// </summary>
    public int DataRetentionDays { get; private set; }

    /// <summary>
    /// Whether real-time stress alerts are included.
    /// </summary>
    public bool HasRealtimeAlerts { get; private set; }

    /// <summary>
    /// Whether weekly summary reports are included.
    /// </summary>
    public bool HasWeeklyReports { get; private set; }

    /// <summary>
    /// Whether data export is allowed.
    /// </summary>
    public bool HasExport { get; private set; }

    /// <summary>
    /// Display order for UI listing.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this plan is currently available for new subscriptions.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }

    private Plan()
    {
    }

    public static Plan Create(
        string name,
        decimal monthlyPrice,
        decimal yearlyPrice,
        string currencyCode,
        int maxUsers,
        int dataRetentionDays,
        bool hasRealtimeAlerts,
        bool hasWeeklyReports,
        bool hasExport,
        int sortOrder,
        string? description = null)
    {
        return new Plan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            CurrencyCode = currencyCode,
            MaxUsers = maxUsers,
            DataRetentionDays = dataRetentionDays,
            HasRealtimeAlerts = hasRealtimeAlerts,
            HasWeeklyReports = hasWeeklyReports,
            HasExport = hasExport,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
