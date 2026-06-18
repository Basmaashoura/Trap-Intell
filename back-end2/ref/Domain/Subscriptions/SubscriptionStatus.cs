namespace Domain.Subscriptions;

/// <summary>
/// Status of an account's subscription.
/// </summary>
public enum SubscriptionStatus
{
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Expired = 4
}
