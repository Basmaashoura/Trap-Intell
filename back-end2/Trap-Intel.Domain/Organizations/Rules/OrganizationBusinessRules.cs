using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Enforces valid organization status transitions.
    /// Implements enterprise policy for organization lifecycle.
    /// </summary>
    public class OrganizationStatusTransitionRule : IBusinessRule
    {
        private static readonly Dictionary<OrganizationStatus, OrganizationStatus[]> ValidTransitions 
            = new()
        {
            // PendingApproval ? Active, Suspended, Inactive
            { OrganizationStatus.PendingApproval, new[] 
                { OrganizationStatus.Active, OrganizationStatus.Suspended, OrganizationStatus.Inactive } },
            
            // Active ? Suspended, Inactive
            { OrganizationStatus.Active, new[] 
                { OrganizationStatus.Suspended, OrganizationStatus.Inactive } },
            
            // Suspended ? Active, Inactive
            { OrganizationStatus.Suspended, new[] 
                { OrganizationStatus.Active, OrganizationStatus.Inactive } },
            
            // Inactive ? PendingApproval (can restart process)
            { OrganizationStatus.Inactive, new[] 
                { OrganizationStatus.PendingApproval } }
        };

        private readonly OrganizationStatus _currentStatus;
        private readonly OrganizationStatus _requestedStatus;

        public OrganizationStatusTransitionRule(
            OrganizationStatus currentStatus,
            OrganizationStatus requestedStatus)
        {
            _currentStatus = currentStatus;
            _requestedStatus = requestedStatus;
        }

        public Error Error => OrganizationErrors.InvalidOperation;

        public bool IsSatisfied()
        {
            if (!ValidTransitions.TryGetValue(_currentStatus, out var allowedTransitions))
                return false;

            return allowedTransitions.Contains(_requestedStatus);
        }
    }

    /// <summary>
    /// Enforces organization deletion requirements.
    /// Ensures organization can be safely deleted.
    /// </summary>
    public class OrganizationDeletionRule : IAsyncBusinessRule
    {
        private readonly Organization _organization;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository? _invoiceRepository;

        public OrganizationDeletionRule(
            Organization organization,
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository? invoiceRepository = null)
        {
            _organization = organization;
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
        }

        public Error Error => OrganizationErrors.CannotDeleteWithActiveSubscriptions;

        public async Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            // Rule 1: Organization must not be active
            if (_organization.Status == OrganizationStatus.Active)
                return false;

            // Rule 2: Cannot have active subscriptions
            var activeSubscriptions = await _subscriptionRepository
                .CountActiveByOrganizationAsync(_organization.Id, cancellationToken);

            if (activeSubscriptions > 0)
                return false;

            // Rule 3: Cannot have pending invoices (if invoice repo available)
            if (_invoiceRepository is not null)
            {
                var pendingInvoices = await _invoiceRepository
                    .GetByOrganizationIdAsync(_organization.Id, cancellationToken);

                // Check for pending invoices
                if (pendingInvoices.Any(i => i.Status == Billing.InvoiceStatus.Draft || i.Status == Billing.InvoiceStatus.Issued || i.Status == Billing.InvoiceStatus.Overdue))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Enforces that organization has required information for approval.
    /// </summary>
    public class OrganizationApprovalRule : IBusinessRule
    {
        private readonly Organization _organization;

        public OrganizationApprovalRule(Organization organization)
        {
            _organization = organization;
        }

        public Error Error => OrganizationErrors.InvalidOperation;

        public bool IsSatisfied()
        {
            // Rule 1: Must be in pending approval status
            if (_organization.Status != OrganizationStatus.PendingApproval)
                return false;

            // Rule 2: Must have valid domain
            if (string.IsNullOrWhiteSpace(_organization.Domain?.Domain))
                return false;

            // Rule 3: Must have contact info
            if (_organization.ContactInfo is null || 
                string.IsNullOrWhiteSpace(_organization.ContactInfo.Email))
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enforces organization can be modified.
    /// Prevents modification of already-approved organizations.
    /// </summary>
    public class OrganizationModificationRule : IBusinessRule
    {
        private readonly Organization _organization;
        private readonly string _propertyName;

        public OrganizationModificationRule(
            Organization organization,
            string propertyName)
        {
            _organization = organization;
            _propertyName = propertyName;
        }

        public Error Error => OrganizationErrors.CannotModifyApprovedOrganization;

        public bool IsSatisfied()
        {
            // Rule 1: Cannot modify core properties if approved
            if (_organization.Status == OrganizationStatus.Active)
            {
                // Core properties that cannot be modified after approval
                var coreProperties = new[] { "Name", "Domain", "Industry", "Size", "Website" };
                
                if (coreProperties.Contains(_propertyName))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Enforces organization hierarchy rules.
    /// Prevents circular references and invalid parent relationships.
    /// </summary>
    public class OrganizationHierarchyRule : IBusinessRule
    {
        private readonly Guid? _parentId;
        private readonly Organization? _parent;

        public OrganizationHierarchyRule(Guid? parentId, Organization? parent)
        {
            _parentId = parentId;
            _parent = parent;
        }

        public bool IsSatisfied()
        {
            // Root organization (no parent) - always valid
            if (_parentId is null) 
                return true;
                
            // Parent doesn't exist - invalid
            if (_parent is null) 
                return false;
                
            // Parent not active - invalid
            if (_parent.Status != OrganizationStatus.Active) 
                return false;
                
            return true;
        }

        public Error Error => Error.Custom(
            "Organization.InvalidParent",
            _parent is null 
                ? "Parent organization does not exist."
                : "Parent organization must be active.");
    }
}
