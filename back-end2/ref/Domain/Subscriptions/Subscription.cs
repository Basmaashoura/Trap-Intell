using SharedKernel;

namespace Domain.Subscriptions;

/// <summary>
/// Represents an account's active subscription to a plan.
/// </summary>
public sealed class Subscription : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The account that owns this subscription.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// The plan this subscription is for.
    /// </summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Current billing cycle.
    /// </summary>
    public BillingCycle BillingCycle { get; private set; }

    /// <summary>
    /// Current status of the subscription.
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>
    /// When the subscription started.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// When the current period ends.
    /// </summary>
    public DateTime CurrentPeriodEnd { get; private set; }

    /// <summary>
    /// When the subscription was cancelled (null if active).
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Navigation property for the plan.
    /// </summary>
    public Plan Plan { get; private set; } = null!;

    /// <summary>
    /// Navigation property for invoices.
    /// </summary>
    public List<Invoice> Invoices { get; private set; } = [];

    private Subscription()
    {
    }

    public static Subscription Create(
        Guid accountId,
        Guid planId,
        BillingCycle billingCycle)
    {
        DateTime now = DateTime.UtcNow;

        return new Subscription
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            BillingCycle = billingCycle,
            Status = SubscriptionStatus.Active,
            StartDate = now,
            CurrentPeriodEnd = billingCycle == BillingCycle.Monthly
                ? now.AddMonths(1)
                : now.AddYears(1),
            CreatedAt = now
        };
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
    }

    public void Renew()
    {
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = BillingCycle == BillingCycle.Monthly
            ? CurrentPeriodEnd.AddMonths(1)
            : CurrentPeriodEnd.AddYears(1);
        CancelledAt = null;
    }

    public void ChangePlan(Guid newPlanId)
    {
        PlanId = newPlanId;
    }

    public void ChangeBillingCycle(BillingCycle newCycle)
    {
        BillingCycle = newCycle;
    }
}
