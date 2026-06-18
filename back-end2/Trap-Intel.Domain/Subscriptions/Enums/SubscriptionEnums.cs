namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Enums specific to the Subscriptions domain.
    /// </summary>

    public enum SubscriptionStatus
    {
        Trial = 0,
        Active = 1,
        Pending = 2,
        Suspended = 3,
        Expired = 4,
        Cancelled = 5
    }

    public enum PaymentMethod
    {
        CreditCard = 0,
        BankTransfer = 1,
        PayPal = 2,
        Crypto = 3,
        Other = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Refunded = 3
    }
}
