using System;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Represents a payment method registered by an organization.
    /// Enterprise-grade payment method management with security considerations.
    /// </summary>
    public class PaymentMethod : AggregateRoot<Guid>
    {
        private PaymentMethod() { }

        private PaymentMethod(
            Guid id,
            Guid organizationId,
            PaymentMethodType type,
            PaymentMethodDetails details)
            : base(id)
        {
            OrganizationId = organizationId;
            Type = type;
            Details = details;
            Status = PaymentMethodStatus.Active;
            IsDefault = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Properties
        public Guid OrganizationId { get; private set; }
        public PaymentMethodType Type { get; private set; }
        public PaymentMethodDetails Details { get; private set; } = null!;
        public PaymentMethodStatus Status { get; private set; }
        public bool IsDefault { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Check if payment method is expired.
        /// </summary>
        public bool IsExpired => Details.IsExpired || Status == PaymentMethodStatus.Expired;

        /// <summary>
        /// Check if payment method is usable (active and not expired).
        /// </summary>
        public bool IsUsable => Status == PaymentMethodStatus.Active && !IsExpired;

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new payment method.
        /// </summary>
        public static Result<PaymentMethod> Create(
            Guid organizationId,
            PaymentMethodType type,
            PaymentMethodDetails details)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<PaymentMethod>(
                    Error.Custom("PaymentMethod.InvalidOrganization", 
                        "Organization ID cannot be empty."));

            if (details is null)
                return Result.Failure<PaymentMethod>(
                    Error.Custom("PaymentMethod.InvalidDetails", 
                        "Payment method details cannot be null."));

            var paymentMethod = new PaymentMethod(
                Guid.NewGuid(),
                organizationId,
                type,
                details);

            paymentMethod.RaiseDomainEvent(new PaymentMethodCreatedEvent(
                paymentMethod.Id,
                organizationId,
                type,
                DateTime.UtcNow));

            return Result.Success(paymentMethod);
        }

        /// <summary>
        /// Factory method to reconstruct payment method from database.
        /// </summary>
        public static PaymentMethod Reconstruct(
            Guid id,
            Guid organizationId,
            PaymentMethodType type,
            PaymentMethodDetails details,
            PaymentMethodStatus status,
            bool isDefault,
            DateTime createdAt,
            DateTime updatedAt)
        {
            var paymentMethod = new PaymentMethod(
                id,
                organizationId,
                type,
                details)
            {
                Status = status,
                IsDefault = isDefault,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            return paymentMethod;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Activate the payment method.
        /// </summary>
        public Result Activate()
        {
            if (Status == PaymentMethodStatus.Active)
                return Result.Failure(
                    Error.Custom("PaymentMethod.AlreadyActive", 
                        "Payment method is already active."));

            if (IsExpired)
                return Result.Failure(
                    Error.Custom("PaymentMethod.Expired", 
                        "Cannot activate an expired payment method."));

            Status = PaymentMethodStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodActivatedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Deactivate the payment method.
        /// </summary>
        public Result Deactivate(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("PaymentMethod.InvalidReason", 
                        "Deactivation reason cannot be empty."));

            if (Status == PaymentMethodStatus.Inactive)
                return Result.Failure(
                    Error.Custom("PaymentMethod.AlreadyInactive", 
                        "Payment method is already inactive."));

            Status = PaymentMethodStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodDeactivatedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Suspend the payment method (temporary hold).
        /// </summary>
        public Result Suspend(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("PaymentMethod.InvalidReason", 
                        "Suspension reason cannot be empty."));

            if (Status == PaymentMethodStatus.Suspended)
                return Result.Failure(
                    Error.Custom("PaymentMethod.AlreadySuspended", 
                        "Payment method is already suspended."));

            Status = PaymentMethodStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodSuspendedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark payment method as expired.
        /// </summary>
        public Result MarkAsExpired()
        {
            if (Status == PaymentMethodStatus.Expired)
                return Result.Failure(
                    Error.Custom("PaymentMethod.AlreadyExpired", 
                        "Payment method is already marked as expired."));

            Status = PaymentMethodStatus.Expired;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodExpiredEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Set this payment method as default for organization.
        /// </summary>
        public void SetAsDefault()
        {
            IsDefault = true;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodSetAsDefaultEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Unset this payment method as default.
        /// </summary>
        public void UnsetAsDefault()
        {
            IsDefault = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Update payment method details (e.g., new billing contact).
        /// </summary>
        public Result UpdateDetails(PaymentMethodDetails newDetails)
        {
            if (newDetails is null)
                return Result.Failure(
                    Error.Custom("PaymentMethod.InvalidDetails", 
                        "Payment method details cannot be null."));

            if (Status == PaymentMethodStatus.Expired)
                return Result.Failure(
                    Error.Custom("PaymentMethod.CannotUpdateExpired", 
                        "Cannot update an expired payment method."));

            Details = newDetails;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentMethodUpdatedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Validate payment method is usable for transactions.
        /// </summary>
        public Result ValidateForUse()
        {
            if (!IsUsable)
                return Result.Failure(
                    Error.Custom("PaymentMethod.NotUsable", 
                        "Payment method is not active or has expired."));

            return Result.Success();
        }

        #endregion

        #region Expiration Management

        /// <summary>
        /// Check if payment method is expiring soon.
        /// </summary>
        public bool IsExpiringSoon(int warningDaysBeforeExpiry = 30)
        {
            if (!Details.ExpiresAt.HasValue) 
                return false;
            
            var warningDate = Details.ExpiresAt.Value.AddDays(-warningDaysBeforeExpiry);
            return DateTime.UtcNow >= warningDate && !IsExpired;
        }

        /// <summary>
        /// Get days until expiration (negative if expired).
        /// </summary>
        public int GetDaysUntilExpiration()
        {
            if (!Details.ExpiresAt.HasValue) 
                return int.MaxValue; // Never expires
                
            return (Details.ExpiresAt.Value - DateTime.UtcNow).Days;
        }

        /// <summary>
        /// Get expiration status message for user display.
        /// </summary>
        public string GetExpirationStatusMessage()
        {
            if (!Details.ExpiresAt.HasValue)
                return "No expiration date";
                
            if (IsExpired)
                return $"Expired {Math.Abs(GetDaysUntilExpiration())} days ago";
                
            var daysUntil = GetDaysUntilExpiration();
            
            if (daysUntil <= 7)
                return $"Expires in {daysUntil} days (urgent)";
            else if (daysUntil <= 30)
                return $"Expires in {daysUntil} days (warning)";
            else
                return $"Expires in {daysUntil} days";
        }

        #endregion

        #region Type Helpers

        /// <summary>
        /// Check if this is a credit card payment method.
        /// </summary>
        public bool IsCreditCard => Type == PaymentMethodType.CreditCard;

        /// <summary>
        /// Check if this is a debit card payment method.
        /// </summary>
        public bool IsDebitCard => Type == PaymentMethodType.DebitCard;

        /// <summary>
        /// Check if this is a bank transfer payment method.
        /// </summary>
        public bool IsBankTransfer => Type == PaymentMethodType.BankTransfer;

        /// <summary>
        /// Check if this is a PayPal payment method.
        /// </summary>
        public bool IsPayPal => Type == PaymentMethodType.PayPal;

        /// <summary>
        /// Check if this is a cryptocurrency payment method.
        /// </summary>
        public bool IsCrypto => Type == PaymentMethodType.Crypto;

        /// <summary>
        /// Check if this is an ACH payment method.
        /// </summary>
        public bool IsACH => Type == PaymentMethodType.ACH;

        /// <summary>
        /// Check if this is a mobile payment method.
        /// </summary>
        public bool IsMobilePayment => Type == PaymentMethodType.MobilePayment;

        #endregion
    }
}
