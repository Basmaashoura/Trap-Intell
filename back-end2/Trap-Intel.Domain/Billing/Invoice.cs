using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Represents an invoice for a subscription billing period.
    /// Enterprise-grade invoice management with full audit trail.
    /// </summary>
    public class Invoice : AggregateRoot<Guid>
    {
        private List<string> _notes = new();

        private Invoice() { }

        private Invoice(
            Guid id,
            Guid subscriptionId,
            Guid organizationId,
            InvoiceNumber invoiceNumber,
            BillingPeriod billingPeriod,
            InvoiceAmount amount,
            UsageDetails usageDetails,
            TaxInfo taxInfo)
            : base(id)
        {
            SubscriptionId = subscriptionId;
            OrganizationId = organizationId;
            InvoiceNumber = invoiceNumber;
            BillingPeriod = billingPeriod;
            Amount = amount;
            UsageDetails = usageDetails;
            TaxInfo = taxInfo;
            Status = InvoiceStatus.Draft;
            IssueDate = null;
            DueDate = null;
            PaymentId = null;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Properties
        public Guid SubscriptionId { get; private set; }
        public Guid OrganizationId { get; private set; }
        public InvoiceNumber InvoiceNumber { get; private set; } = null!;
        public InvoiceStatus Status { get; private set; }
        public BillingPeriod BillingPeriod { get; private set; } = null!;
        public InvoiceAmount Amount { get; private set; } = null!;
        public UsageDetails UsageDetails { get; private set; } = null!;
        public TaxInfo TaxInfo { get; private set; } = null!;
        public DateTime? IssueDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public Guid? PaymentId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();

        public bool HasLateFeeApplied =>
            _notes.Any(note => note.StartsWith("Late fee applied:", StringComparison.OrdinalIgnoreCase));

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new invoice.
        /// </summary>
        public static Result<Invoice> Create(
            Guid subscriptionId,
            Guid organizationId,
            InvoiceNumber invoiceNumber,
            BillingPeriod billingPeriod,
            InvoiceAmount amount,
            UsageDetails usageDetails,
            TaxInfo taxInfo)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidSubscription", "Subscription ID cannot be empty."));

            if (organizationId == Guid.Empty)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidOrganization", "Organization ID cannot be empty."));

            if (invoiceNumber is null)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidNumber", "Invoice number cannot be null."));

            if (billingPeriod is null)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidPeriod", "Billing period cannot be null."));

            if (amount is null)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidAmount", "Amount cannot be null."));

            if (usageDetails is null)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidUsage", "Usage details cannot be null."));

            if (taxInfo is null)
                return Result.Failure<Invoice>(
                    Error.Custom("Invoice.InvalidTax", "Tax info cannot be null."));

            var invoice = new Invoice(
                Guid.NewGuid(),
                subscriptionId,
                organizationId,
                invoiceNumber,
                billingPeriod,
                amount,
                usageDetails,
                taxInfo);

            invoice.RaiseDomainEvent(new InvoiceCreatedEvent(
                invoice.Id,
                subscriptionId,
                organizationId,
                invoiceNumber.Value,
                amount.TotalAmount,
                DateTime.UtcNow));

            return Result.Success(invoice);
        }

        /// <summary>
        /// Factory method to reconstruct invoice from database.
        /// </summary>
        public static Invoice Reconstruct(
            Guid id,
            Guid subscriptionId,
            Guid organizationId,
            InvoiceNumber invoiceNumber,
            InvoiceStatus status,
            BillingPeriod billingPeriod,
            InvoiceAmount amount,
            UsageDetails usageDetails,
            TaxInfo taxInfo,
            DateTime? issueDate,
            DateTime? dueDate,
            Guid? paymentId,
            DateTime createdAt,
            DateTime updatedAt,
            List<string>? notes = null)
        {
            var invoice = new Invoice(
                id,
                subscriptionId,
                organizationId,
                invoiceNumber,
                billingPeriod,
                amount,
                usageDetails,
                taxInfo)
            {
                Status = status,
                IssueDate = issueDate,
                DueDate = dueDate,
                PaymentId = paymentId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                _notes = notes ?? new()
            };

            return invoice;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Issue the invoice (change from Draft to Issued).
        /// </summary>
        public Result Issue(int daysDue = 30)
        {
            if (Status != InvoiceStatus.Draft)
                return Result.Failure(
                    Error.Custom("Invoice.CannotIssue", "Only draft invoices can be issued."));

            if (daysDue <= 0)
                return Result.Failure(
                    Error.Custom("Invoice.InvalidDueDays", "Due days must be greater than zero."));

            Status = InvoiceStatus.Issued;
            IssueDate = DateTime.UtcNow;
            DueDate = DateTime.UtcNow.AddDays(daysDue);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new InvoiceIssuedEvent(
                Id,
                SubscriptionId,
                IssueDate.Value,
                DueDate.Value,
                Amount.TotalAmount,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark invoice as paid.
        /// </summary>
        public Result MarkAsPaid(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Invoice.InvalidPaymentId", "Payment ID cannot be empty."));

            if (Status != InvoiceStatus.Issued && Status != InvoiceStatus.Overdue)
                return Result.Failure(
                    Error.Custom("Invoice.CannotMarkPaid", 
                        "Only issued or overdue invoices can be marked as paid."));

            Status = InvoiceStatus.Paid;
            PaymentId = paymentId;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new InvoicePaidEvent(
                Id,
                SubscriptionId,
                paymentId,
                Amount.TotalAmount,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark invoice as overdue.
        /// </summary>
        public Result MarkAsOverdue()
        {
            if (Status != InvoiceStatus.Issued)
                return Result.Failure(
                    Error.Custom("Invoice.CannotMarkOverdue", 
                        "Only issued invoices can be marked as overdue."));

            if (!DueDate.HasValue || DateTime.UtcNow <= DueDate.Value)
                return Result.Failure(
                    Error.Custom("Invoice.NotOverdue", 
                        "Invoice due date has not passed yet."));

            Status = InvoiceStatus.Overdue;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new InvoiceOverdueEvent(
                Id,
                SubscriptionId,
                Amount.TotalAmount,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Cancel the invoice.
        /// </summary>
        public Result Cancel(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Invoice.InvalidReason", "Cancellation reason cannot be empty."));

            if (Status == InvoiceStatus.Paid)
                return Result.Failure(
                    Error.Custom("Invoice.CannotCancelPaid", 
                        "Paid invoices cannot be cancelled. Use refund instead."));

            if (Status == InvoiceStatus.Cancelled || Status == InvoiceStatus.Refunded)
                return Result.Failure(
                    Error.Custom("Invoice.AlreadyCancelled", "Invoice is already cancelled or refunded."));

            Status = InvoiceStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;

            _notes.Add($"Cancelled: {reason}");

            RaiseDomainEvent(new InvoiceCancelledEvent(
                Id,
                SubscriptionId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Refund the invoice (if paid).
        /// </summary>
        public Result Refund(decimal refundAmount, string reason)
        {
            if (refundAmount <= 0)
                return Result.Failure(
                    Error.Custom("Invoice.InvalidRefundAmount", "Refund amount must be greater than zero."));

            if (refundAmount > Amount.TotalAmount)
                return Result.Failure(
                    Error.Custom("Invoice.RefundExceedsAmount", 
                        "Refund amount cannot exceed invoice total."));

            if (Status != InvoiceStatus.Paid)
                return Result.Failure(
                    Error.Custom("Invoice.CannotRefund", "Only paid invoices can be refunded."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Invoice.InvalidReason", "Refund reason cannot be empty."));

            Status = InvoiceStatus.Refunded;
            UpdatedAt = DateTime.UtcNow;

            _notes.Add($"Refunded: {refundAmount} ({reason})");

            RaiseDomainEvent(new InvoiceRefundedEvent(
                Id,
                SubscriptionId,
                refundAmount,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Add note to invoice.
        /// </summary>
        public Result AddNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return Result.Failure(BillingErrors.InvalidNote);

            if (note.Length > 1000)
                return Result.Failure(BillingErrors.NoteTooLong);

            _notes.Add(note.Trim());
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Update usage details.
        /// </summary>
        public Result UpdateUsageDetails(UsageDetails newUsageDetails)
        {
            if (newUsageDetails is null)
                return Result.Failure(
                    Error.Custom("Invoice.InvalidUsageDetails", "Usage details cannot be null."));

            if (Status != InvoiceStatus.Draft)
                return Result.Failure(
                    Error.Custom("Invoice.CannotUpdateUsage", 
                        "Usage details can only be updated for draft invoices."));

            UsageDetails = newUsageDetails;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Update invoice amount.
        /// </summary>
        public Result UpdateAmount(InvoiceAmount newAmount)
        {
            if (newAmount is null)
                return Result.Failure(
                    Error.Custom("Invoice.InvalidAmount", "Amount cannot be null."));

            if (Status != InvoiceStatus.Draft)
                return Result.Failure(
                    Error.Custom("Invoice.CannotUpdateAmount", 
                        "Amount can only be updated for draft invoices."));

            Amount = newAmount;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        #endregion

        #region Late Fee Management

        /// <summary>
        /// Calculate late fee for overdue invoice.
        /// </summary>
        public decimal CalculateLateFee(
            decimal lateFeePercent = 5,
            int gracePeriodDays = 7)
        {
            if (Status != InvoiceStatus.Overdue) 
                return 0;
                
            if (!DueDate.HasValue) 
                return 0;
            
            var daysOverdue = (DateTime.UtcNow - DueDate.Value).Days;
            
            // Grace period - no late fee yet
            if (daysOverdue <= gracePeriodDays) 
                return 0;
            
            // Calculate late fee (5% of total amount by default)
            return Math.Round(Amount.TotalAmount * (lateFeePercent / 100), 2);
        }

        /// <summary>
        /// Apply late fee to invoice amount.
        /// </summary>
        public Result ApplyLateFee(decimal lateFeePercent = 5)
        {
            if (Status != InvoiceStatus.Overdue)
                return Result.Failure(
                    Error.Custom("Invoice.NotOverdue", "Late fees can only be applied to overdue invoices."));

            if (HasLateFeeApplied)
                return Result.Success();
                
            var lateFee = CalculateLateFee(lateFeePercent);
            
            if (lateFee <= 0) 
                return Result.Success(); // No late fee to apply
            
            // Create new amount with late fee added as overage
            var newAmount = new InvoiceAmount(
                Amount.BaseAmount,
                Amount.OverageAmount + lateFee,  // Add late fee to overage
                Amount.TaxAmount,
                Amount.Discount,
                Amount.Currency);
                
            Amount = newAmount;
            UpdatedAt = DateTime.UtcNow;
            
            _notes.Add($"Late fee applied: ${lateFee:F2} ({lateFeePercent}% of total)");
            
            RaiseDomainEvent(new InvoiceLateFeeAppliedEvent(
                Id,
                SubscriptionId,
                lateFee,
                DateTime.UtcNow));
                
            return Result.Success();
        }

        /// <summary>
        /// Get days overdue.
        /// </summary>
        public int GetDaysOverdue()
        {
            if (!DueDate.HasValue || DateTime.UtcNow <= DueDate.Value)
                return 0;
                
            return (DateTime.UtcNow - DueDate.Value).Days;
        }

        #endregion
    }
}
