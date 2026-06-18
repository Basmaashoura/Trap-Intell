namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Enums specific to the Billing domain.
    /// </summary>

    public enum InvoiceStatus
    {
        Draft = 0,
        Issued = 1,
        Paid = 2,
        Overdue = 3,
        Cancelled = 4,
        Refunded = 5
    }

    public enum PaymentMethodType
    {
        CreditCard = 0,
        DebitCard = 1,
        BankTransfer = 2,
        PayPal = 3,
        Crypto = 4,
        ACH = 5,
        MobilePayment = 6
    }

    public enum PaymentMethodStatus
    {
        Active = 0,
        Inactive = 1,
        Expired = 2,
        Suspended = 3
    }
}
