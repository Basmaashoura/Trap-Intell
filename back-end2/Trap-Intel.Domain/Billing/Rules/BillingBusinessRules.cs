using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Business rules for the Billing domain.
    /// Enforce invariants and complex policies.
    /// </summary>

    /// <summary>
    /// Rule: Invoice can only be issued if it has valid amounts.
    /// </summary>
    public class InvoiceIssuanceRule : IBusinessRule
    {
        private readonly Invoice _invoice;
        public Error Error { get; }

        public InvoiceIssuanceRule(Invoice invoice)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            Error = BillingErrors.InvoiceCannotIssue;
        }

        public bool IsSatisfied()
        {
            // Can only issue draft invoices
            if (_invoice.Status != InvoiceStatus.Draft)
                return false;

            // Must have valid amount
            if (_invoice.Amount.TotalAmount <= 0)
                return false;

            // Must have subscription ID
            if (_invoice.SubscriptionId == Guid.Empty)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Rule: Payment can only be made for invoices in specific statuses.
    /// </summary>
    public class InvoicePaymentRule : IBusinessRule
    {
        private readonly Invoice _invoice;
        public Error Error { get; }

        public InvoicePaymentRule(Invoice invoice)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            Error = BillingErrors.InvoiceCannotMarkPaid;
        }

        public bool IsSatisfied()
        {
            // Only issued or overdue invoices can be paid
            return _invoice.Status == InvoiceStatus.Issued || 
                   _invoice.Status == InvoiceStatus.Overdue;
        }
    }

    /// <summary>
    /// Rule: Invoice can only be refunded if it's paid.
    /// </summary>
    public class InvoiceRefundRule : IBusinessRule
    {
        private readonly Invoice _invoice;
        private readonly decimal _refundAmount;
        public Error Error { get; }

        public InvoiceRefundRule(Invoice invoice, decimal refundAmount)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            _refundAmount = refundAmount;
            
            if (_invoice.Status != InvoiceStatus.Paid)
                Error = BillingErrors.InvoiceCannotRefund;
            else if (_refundAmount > _invoice.Amount.TotalAmount)
                Error = BillingErrors.InvoiceRefundExceedsAmount;
            else
                Error = null!;
        }

        public bool IsSatisfied()
        {
            if (_invoice.Status != InvoiceStatus.Paid)
                return false;

            if (_refundAmount <= 0)
                return false;

            if (_refundAmount > _invoice.Amount.TotalAmount)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Rule: Invoice can only be marked as overdue if due date has passed.
    /// </summary>
    public class InvoiceOverdueRule : IBusinessRule
    {
        private readonly Invoice _invoice;
        public Error Error { get; }

        public InvoiceOverdueRule(Invoice invoice)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            Error = BillingErrors.InvoiceInvalidStatus;
        }

        public bool IsSatisfied()
        {
            // Must be issued
            if (_invoice.Status != InvoiceStatus.Issued)
                return false;

            // Must have due date
            if (!_invoice.DueDate.HasValue)
                return false;

            // Due date must have passed
            if (DateTime.UtcNow <= _invoice.DueDate.Value)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Rule: Payment method can only be used if active and not expired.
    /// </summary>
    public class PaymentMethodUsabilityRule : IBusinessRule
    {
        private readonly PaymentMethod _paymentMethod;
        public Error Error { get; }

        public PaymentMethodUsabilityRule(PaymentMethod paymentMethod)
        {
            _paymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            Error = BillingErrors.PaymentMethodNotUsable;
        }

        public bool IsSatisfied()
        {
            // Must be active
            if (_paymentMethod.Status != PaymentMethodStatus.Active)
                return false;

            // Must not be expired
            if (_paymentMethod.IsExpired)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Async rule: Payment method can be charged (check with external processor).
    /// </summary>
    public class PaymentMethodChargeableRule : IAsyncBusinessRule
    {
        private readonly PaymentMethod _paymentMethod;
        public Error Error { get; }

        public PaymentMethodChargeableRule(PaymentMethod paymentMethod)
        {
            _paymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            Error = BillingErrors.PaymentMethodNotUsable;
        }

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Must pass usability check
            var usabilityRule = new PaymentMethodUsabilityRule(_paymentMethod);
            if (!usabilityRule.IsSatisfied())
                return false;

            // TODO: Check with payment processor if card is valid
            // This would be an external API call
            // For now, we'll just return true
            await Task.Delay(0, cancellationToken);

            return true;
        }
    }

    /// <summary>
    /// Rule: Organization can have at most one default payment method.
    /// </summary>
    public class DefaultPaymentMethodRule : IBusinessRule
    {
        private readonly PaymentMethod _paymentMethod;
        public Error Error { get; }

        public DefaultPaymentMethodRule(PaymentMethod paymentMethod)
        {
            _paymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            Error = BillingErrors.InvalidOperation;
        }

        public bool IsSatisfied()
        {
            // Can only set active payment methods as default
            if (_paymentMethod.Status != PaymentMethodStatus.Active)
                return false;

            // Cannot set expired as default
            if (_paymentMethod.IsExpired)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Rule: Invoice must have valid tax calculation.
    /// </summary>
    public class InvoiceTaxCalculationRule : IBusinessRule
    {
        private readonly Invoice _invoice;
        public Error Error { get; }

        public InvoiceTaxCalculationRule(Invoice invoice)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            Error = BillingErrors.InvoiceInvalidAmount;
        }

        public bool IsSatisfied()
        {
            // Tax must be non-negative
            if (_invoice.Amount.TaxAmount < 0)
                return false;

            // Tax rate must be valid
            if (_invoice.TaxInfo.TaxRate < 0 || _invoice.TaxInfo.TaxRate > 1)
                return false;

            // Total must be greater than or equal to base amount
            if (_invoice.Amount.TotalAmount < _invoice.Amount.BaseAmount)
                return false;

            return true;
        }
    }
}
