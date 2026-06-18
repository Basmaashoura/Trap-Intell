using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Error definitions for the Billing domain.
    /// Semantic error codes for type-safe error handling.
    /// </summary>
    public static class BillingErrors
    {
        // Invoice Errors
        public static readonly Error InvoiceNotFound = Error.Custom(
            "Invoice.NotFound",
            "The specified invoice does not exist.");

        public static readonly Error InvoiceInvalidStatus = Error.Custom(
            "Invoice.InvalidStatus",
            "The invoice is in an invalid status for this operation.");

        public static readonly Error InvoiceCannotIssue = Error.Custom(
            "Invoice.CannotIssue",
            "Only draft invoices can be issued.");

        public static readonly Error InvoiceCannotMarkPaid = Error.Custom(
            "Invoice.CannotMarkPaid",
            "Only issued or overdue invoices can be marked as paid.");

        public static readonly Error InvoiceCannotRefund = Error.Custom(
            "Invoice.CannotRefund",
            "Only paid invoices can be refunded.");

        public static readonly Error InvoiceRefundExceedsAmount = Error.Custom(
            "Invoice.RefundExceedsAmount",
            "Refund amount cannot exceed invoice total.");

        public static readonly Error InvoiceAlreadyCancelled = Error.Custom(
            "Invoice.AlreadyCancelled",
            "Invoice is already cancelled or refunded.");

        public static readonly Error InvoiceCannotCancelPaid = Error.Custom(
            "Invoice.CannotCancelPaid",
            "Paid invoices cannot be cancelled. Use refund instead.");

        public static readonly Error InvoiceInvalidNumber = Error.Custom(
            "Invoice.InvalidNumber",
            "Invoice number is invalid.");

        public static readonly Error InvoiceInvalidPeriod = Error.Custom(
            "Invoice.InvalidPeriod",
            "Billing period is invalid.");

        public static readonly Error InvoiceInvalidAmount = Error.Custom(
            "Invoice.InvalidAmount",
            "Invoice amount is invalid.");

        public static readonly Error InvoiceInvalidUsageDetails = Error.Custom(
            "Invoice.InvalidUsageDetails",
            "Usage details are invalid.");

        public static readonly Error InvoiceCannotUpdateUsage = Error.Custom(
            "Invoice.CannotUpdateUsage",
            "Usage details can only be updated for draft invoices.");

        public static readonly Error InvoiceCannotUpdateAmount = Error.Custom(
            "Invoice.CannotUpdateAmount",
            "Amount can only be updated for draft invoices.");

        // PaymentMethod Errors
        public static readonly Error PaymentMethodNotFound = Error.Custom(
            "PaymentMethod.NotFound",
            "The specified payment method does not exist.");

        public static readonly Error PaymentMethodExpired = Error.Custom(
            "PaymentMethod.Expired",
            "The payment method has expired.");

        public static readonly Error PaymentMethodNotUsable = Error.Custom(
            "PaymentMethod.NotUsable",
            "The payment method is not active or has expired.");

        public static readonly Error PaymentMethodAlreadyActive = Error.Custom(
            "PaymentMethod.AlreadyActive",
            "The payment method is already active.");

        public static readonly Error PaymentMethodAlreadyInactive = Error.Custom(
            "PaymentMethod.AlreadyInactive",
            "The payment method is already inactive.");

        public static readonly Error PaymentMethodAlreadySuspended = Error.Custom(
            "PaymentMethod.AlreadySuspended",
            "The payment method is already suspended.");

        public static readonly Error PaymentMethodAlreadyExpired = Error.Custom(
            "PaymentMethod.AlreadyExpired",
            "The payment method is already marked as expired.");

        public static readonly Error PaymentMethodCannotUpdateExpired = Error.Custom(
            "PaymentMethod.CannotUpdateExpired",
            "Cannot update an expired payment method.");

        public static readonly Error PaymentMethodInvalidDetails = Error.Custom(
            "PaymentMethod.InvalidDetails",
            "Payment method details are invalid.");

        public static readonly Error PaymentMethodCannotActivateExpired = Error.Custom(
            "PaymentMethod.CannotActivateExpired",
            "Cannot activate an expired payment method.");

        public static readonly Error PaymentMethodNoDefault = Error.Custom(
            "PaymentMethod.NoDefault",
            "No default payment method is configured.");

        public static readonly Error PaymentMethodDefaultConflict = Error.Custom(
            "PaymentMethod.DefaultConflict",
            "Another default payment method update is in progress. Please retry.");

        public static readonly Error PaymentMethodInvalidType = Error.Custom(
            "PaymentMethod.InvalidType",
            "The payment method type is invalid.");

        // General Billing Errors
        public static readonly Error InvalidOperation = Error.Custom(
            "Billing.InvalidOperation",
            "The requested billing operation is not valid.");

        public static Error InvoiceNotFound_Detail(string invoiceId)
        {
            return Error.Custom("Invoice.NotFound", $"Invoice '{invoiceId}' not found.");
        }

        public static Error PaymentMethodNotFound_Detail(string methodId)
        {
            return Error.Custom("PaymentMethod.NotFound", $"Payment method '{methodId}' not found.");
        }

        public static readonly Error InvalidNote = Error.Custom(
            "Invoice.InvalidNote",
            "Note cannot be empty.");

        public static readonly Error NoteTooLong = Error.Custom(
            "Invoice.NoteTooLong",
            "Note cannot exceed 1000 characters.");
    }
}
