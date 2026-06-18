using System;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared.ValueObjects;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Value objects for the Billing domain.
    /// Immutable, self-validating domain concepts.
    /// </summary>

    /// <summary>
    /// Represents an invoice number with validation.
    /// </summary>
    public record InvoiceNumber
    {
        public string Value { get; }

        private InvoiceNumber(string value)
        {
            Value = value;
        }

        public static Result<InvoiceNumber> Create(string? number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return Result.Failure<InvoiceNumber>(
                    Error.Custom("Invoice.InvalidNumber", "Invoice number cannot be empty."));

            var trimmed = number.Trim().ToUpperInvariant();

            if (trimmed.Length < 5)
                return Result.Failure<InvoiceNumber>(
                    Error.Custom("Invoice.NumberTooShort", "Invoice number must be at least 5 characters."));

            if (trimmed.Length > 50)
                return Result.Failure<InvoiceNumber>(
                    Error.Custom("Invoice.NumberTooLong", "Invoice number cannot exceed 50 characters."));

            return Result.Success(new InvoiceNumber(trimmed));
        }
    }

    /// <summary>
    /// Represents billing period (start and end dates).
    /// </summary>
    public record BillingPeriod
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public BillingPeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date.", nameof(startDate));

            StartDate = startDate;
            EndDate = endDate;
        }

        public int DaysInPeriod => (int)(EndDate - StartDate).TotalDays;

        public decimal GetDailyRate(decimal totalAmount)
        {
            return DaysInPeriod > 0 ? totalAmount / DaysInPeriod : 0;
        }

        /// <summary>
        /// Get daily rate using Money value object.
        /// </summary>
        public Money GetDailyRate(Money totalAmount)
        {
            if (DaysInPeriod <= 0)
                return Money.Zero(totalAmount.Currency);
            return totalAmount / DaysInPeriod;
        }
    }

    /// <summary>
    /// Represents invoice amounts (base, overage, tax, total).
    /// </summary>
    public record InvoiceAmount
    {
        public decimal BaseAmount { get; }
        public decimal OverageAmount { get; }
        public decimal TaxAmount { get; }
        public decimal Discount { get; }
        public string Currency { get; }

        public decimal TotalAmount => BaseAmount + OverageAmount + TaxAmount - Discount;

        public InvoiceAmount(
            decimal baseAmount,
            decimal overageAmount = 0,
            decimal taxAmount = 0,
            decimal discount = 0,
            string currency = "USD")
        {
            if (baseAmount < 0)
                throw new ArgumentException("Base amount cannot be negative.", nameof(baseAmount));

            if (overageAmount < 0)
                throw new ArgumentException("Overage amount cannot be negative.", nameof(overageAmount));

            if (taxAmount < 0)
                throw new ArgumentException("Tax amount cannot be negative.", nameof(taxAmount));

            if (discount < 0)
                throw new ArgumentException("Discount cannot be negative.", nameof(discount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));

            BaseAmount = baseAmount;
            OverageAmount = overageAmount;
            TaxAmount = taxAmount;
            Discount = discount;
            Currency = currency;
        }

        /// <summary>
        /// Create from Money value objects.
        /// </summary>
        public static InvoiceAmount FromMoney(
            Money baseAmount,
            Money? overageAmount = null,
            Money? taxAmount = null,
            Money? discount = null)
        {
            var currency = baseAmount.Currency.Code;
            return new InvoiceAmount(
                baseAmount.Amount,
                overageAmount?.Amount ?? 0,
                taxAmount?.Amount ?? 0,
                discount?.Amount ?? 0,
                currency);
        }

        /// <summary>
        /// Convert to Money value object for the total.
        /// </summary>
        public Money ToMoney()
        {
            var currencyResult = Shared.ValueObjects.Currency.Create(Currency);
            if (currencyResult.IsFailure)
                return Money.USD(TotalAmount);
            return Money.Create(TotalAmount, currencyResult.Value).Value;
        }

        /// <summary>
        /// Get base amount as Money.
        /// </summary>
        public Money GetBaseAsMoney() => Money.Create(BaseAmount, Currency).Value;

        /// <summary>
        /// Get overage amount as Money.
        /// </summary>
        public Money GetOverageAsMoney() => Money.Create(OverageAmount, Currency).Value;

        /// <summary>
        /// Get tax amount as Money.
        /// </summary>
        public Money GetTaxAsMoney() => Money.Create(TaxAmount, Currency).Value;

        /// <summary>
        /// Get discount as Money.
        /// </summary>
        public Money GetDiscountAsMoney() => Money.Create(Discount, Currency).Value;
    }

    /// <summary>
    /// Represents usage details captured in invoice.
    /// </summary>
    public record UsageDetails
    {
        public int HoneypotsUsed { get; }
        public decimal StorageUsedGb { get; }
        public decimal OverageCharges { get; }

        public UsageDetails(
            int honeypotsUsed,
            decimal storageUsedGb,
            decimal overageCharges = 0)
        {
            if (honeypotsUsed < 0)
                throw new ArgumentException("Honeypots used cannot be negative.", nameof(honeypotsUsed));

            if (storageUsedGb < 0)
                throw new ArgumentException("Storage used cannot be negative.", nameof(storageUsedGb));

            if (overageCharges < 0)
                throw new ArgumentException("Overage charges cannot be negative.", nameof(overageCharges));

            HoneypotsUsed = honeypotsUsed;
            StorageUsedGb = storageUsedGb;
            OverageCharges = overageCharges;
        }

        /// <summary>
        /// Get overage charges as Money.
        /// </summary>
        public Money GetOverageChargesAsMoney(string currency = "USD")
        {
            var result = Money.Create(OverageCharges, currency);
            return result.IsSuccess ? result.Value : Money.USD(OverageCharges);
        }
    }

    /// <summary>
    /// Represents payment method details (card, bank, etc).
    /// </summary>
    public record PaymentMethodDetails
    {
        public string? LastFourDigits { get; }
        public string? CardBrand { get; }
        public string? PaymentProcessor { get; }
        public string? Token { get; }
        public DateTime? ExpiresAt { get; }
        public string? BillingContactEmail { get; }

        public PaymentMethodDetails(
            string? lastFourDigits = null,
            string? cardBrand = null,
            string? paymentProcessor = null,
            string? token = null,
            DateTime? expiresAt = null,
            string? billingContactEmail = null)
        {
            if (lastFourDigits is not null && lastFourDigits.Length != 4)
                throw new ArgumentException("Last four digits must be exactly 4 characters.", nameof(lastFourDigits));

            if (expiresAt.HasValue && expiresAt < DateTime.UtcNow)
                throw new ArgumentException("Expiration date cannot be in the past.", nameof(expiresAt));

            if (billingContactEmail is not null && !billingContactEmail.Contains("@"))
                throw new ArgumentException("Invalid email format.", nameof(billingContactEmail));

            LastFourDigits = lastFourDigits;
            CardBrand = cardBrand;
            PaymentProcessor = paymentProcessor;
            Token = token;
            ExpiresAt = expiresAt;
            BillingContactEmail = billingContactEmail;
        }

        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    }

    /// <summary>
    /// Represents tax information for invoice.
    /// </summary>
    public record TaxInfo
    {
        public string? TaxId { get; }
        public decimal TaxRate { get; }

        public TaxInfo(string? taxId = null, decimal taxRate = 0)
        {
            if (taxRate < 0 || taxRate > 1)
                throw new ArgumentException("Tax rate must be between 0 and 1.", nameof(taxRate));

            TaxId = taxId;
            TaxRate = taxRate;
        }

        /// <summary>
        /// Calculate tax on an amount.
        /// </summary>
        public Money CalculateTax(Money amount)
        {
            return amount.Percentage(TaxRate * 100);
        }

        /// <summary>
        /// Get tax amount from Money value.
        /// </summary>
        public TaxAmount ToTaxAmount(Money netAmount, string taxName = "Tax")
        {
            return new TaxAmount(netAmount, TaxRate * 100, taxName);
        }
    }

    /// <summary>
    /// Represents a payment transaction.
    /// </summary>
    public record PaymentTransaction
    {
        /// <summary>
        /// Transaction ID from payment processor.
        /// </summary>
        public string TransactionId { get; init; }

        /// <summary>
        /// Amount charged.
        /// </summary>
        public Money Amount { get; init; }

        /// <summary>
        /// Transaction status.
        /// </summary>
        public PaymentTransactionStatus Status { get; init; }

        /// <summary>
        /// When transaction occurred.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Payment processor used.
        /// </summary>
        public string Processor { get; init; }

        /// <summary>
        /// Error message if failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Refund amount (if partially refunded).
        /// </summary>
        public Money? RefundedAmount { get; init; }

        public PaymentTransaction(
            string transactionId,
            Money amount,
            PaymentTransactionStatus status,
            DateTime timestamp,
            string processor,
            string? errorMessage = null,
            Money? refundedAmount = null)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            Amount = amount ?? throw new ArgumentNullException(nameof(amount));
            Status = status;
            Timestamp = timestamp;
            Processor = processor ?? "Unknown";
            ErrorMessage = errorMessage;
            RefundedAmount = refundedAmount;
        }

        /// <summary>
        /// Check if transaction is successful.
        /// </summary>
        public bool IsSuccessful => Status == PaymentTransactionStatus.Completed;

        /// <summary>
        /// Get net amount (after refunds).
        /// </summary>
        public Money GetNetAmount()
        {
            if (RefundedAmount == null) return Amount;
            return Amount - RefundedAmount;
        }
    }

    /// <summary>
    /// Payment transaction status.
    /// </summary>
    public enum PaymentTransactionStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5,
        PartiallyRefunded = 6
    }
}
