using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Error codes and factory methods for the Organizations domain.
    /// Follows DDD error handling patterns with semantic error codes.
    /// </summary>
    public static class OrganizationErrors
    {
        // Name validation errors
        public static readonly Error InvalidName = Error.Custom(
            "Organization.InvalidName",
            "Organization name cannot be empty or exceed 255 characters.");

        public static readonly Error NameTooShort = Error.Custom(
            "Organization.NameTooShort",
            "Organization name must be at least 3 characters long.");

        public static readonly Error DuplicateName = Error.Custom(
            "Organization.DuplicateName",
            "An organization with this name already exists.");

        // Industry validation errors
        public static readonly Error InvalidIndustry = Error.Custom(
            "Organization.InvalidIndustry",
            "Organization industry cannot be empty.");

        public static readonly Error IndustryTooShort = Error.Custom(
            "Organization.IndustryTooShort",
            "Industry must be at least 2 characters long.");

        // Size validation errors
        public static readonly Error InvalidSize = Error.Custom(
            "Organization.InvalidSize",
            "Organization size must be greater than zero.");

        public static readonly Error SizeOutOfRange = Error.Custom(
            "Organization.SizeOutOfRange",
            "Organization size must be between 1 and 1,000,000.");

        // Website validation errors
        public static readonly Error InvalidWebsite = Error.Custom(
            "Organization.InvalidWebsite",
            "Organization website cannot be empty.");

        public static readonly Error InvalidWebsiteFormat = Error.Custom(
            "Organization.InvalidWebsiteFormat",
            "Website must be a valid URL.");

        public static readonly Error DuplicateWebsite = Error.Custom(
            "Organization.DuplicateWebsite",
            "An organization with this website already exists.");

        // Domain validation errors
        public static readonly Error InvalidDomain = Error.Custom(
            "Organization.InvalidDomain",
            "Organization domain cannot be empty.");

        public static readonly Error InvalidDomainFormat = Error.Custom(
            "Organization.InvalidDomainFormat",
            "Domain must be a valid domain name.");

        public static readonly Error DuplicateDomain = Error.Custom(
            "Organization.DuplicateDomain",
            "An organization with this domain already exists.");

        // Contact info validation errors
        public static readonly Error InvalidContactInfo = Error.Custom(
            "Organization.InvalidContactInfo",
            "Contact information is invalid.");

        public static readonly Error InvalidEmail = Error.Custom(
            "Organization.InvalidEmail",
            "Email address is not valid.");

        public static readonly Error InvalidPhone = Error.Custom(
            "Organization.InvalidPhone",
            "Phone number is not valid.");

        // Tax ID validation errors
        public static readonly Error InvalidTaxId = Error.Custom(
            "Organization.InvalidTaxId",
            "Tax ID is invalid.");

        public static readonly Error DuplicateTaxId = Error.Custom(
            "Organization.DuplicateTaxId",
            "An organization with this tax ID already exists.");

        // Address validation errors
        public static readonly Error InvalidAddress = Error.Custom(
            "Organization.InvalidAddress",
            "Address information is invalid.");

        public static readonly Error InvalidStreet = Error.Custom(
            "Organization.InvalidStreet",
            "Street address cannot be empty.");

        public static readonly Error InvalidCity = Error.Custom(
            "Organization.InvalidCity",
            "City cannot be empty.");

        public static readonly Error InvalidState = Error.Custom(
            "Organization.InvalidState",
            "State/Province cannot be empty.");

        public static readonly Error InvalidPostalCode = Error.Custom(
            "Organization.InvalidPostalCode",
            "Postal code cannot be empty.");

        public static readonly Error InvalidCountry = Error.Custom(
            "Organization.InvalidCountry",
            "Country cannot be empty.");

        public static readonly Error MultipleAddressesNotAllowed = Error.Custom(
            "Organization.MultipleAddressesNotAllowed",
            "This organization does not allow multiple addresses.");

        public static readonly Error AddressNotFound = Error.Custom(
            "Organization.AddressNotFound",
            "The specified address was not found.");

        public static readonly Error CannotRemoveLastAddress = Error.Custom(
            "Organization.CannotRemoveLastAddress",
            "Cannot remove the last address from an organization.");

        // Status validation errors
        public static readonly Error AlreadyApproved = Error.Custom(
            "Organization.AlreadyApproved",
            "Organization is already approved.");

        public static readonly Error AlreadyActive = Error.Custom(
            "Organization.AlreadyActive",
            "Organization is already active.");

        public static readonly Error AlreadySuspended = Error.Custom(
            "Organization.AlreadySuspended",
            "Organization is already suspended.");

        public static readonly Error AlreadyInactive = Error.Custom(
            "Organization.AlreadyInactive",
            "Organization is already inactive.");

        public static readonly Error CannotApproveSuspended = Error.Custom(
            "Organization.CannotApproveSuspended",
            "Cannot approve a suspended organization.");

        public static readonly Error CannotApproveInactive = Error.Custom(
            "Organization.CannotApproveInactive",
            "Cannot approve an inactive organization.");

        public static readonly Error CannotSuspendApproved = Error.Custom(
            "Organization.CannotSuspendApproved",
            "Cannot suspend an approved organization without reason.");

        public static readonly Error CannotActivateSuspended = Error.Custom(
            "Organization.CannotActivateSuspended",
            "Cannot activate a suspended organization. Contact support.");

        // Approval errors
        public static readonly Error InvalidApprovalReason = Error.Custom(
            "Organization.InvalidApprovalReason",
            "Approval reason cannot be empty.");

        public static readonly Error ApprovalReasonTooShort = Error.Custom(
            "Organization.ApprovalReasonTooShort",
            "Approval reason must be at least 5 characters long.");

        public static readonly Error ApprovalReasonTooLong = Error.Custom(
            "Organization.ApprovalReasonTooLong",
            "Approval reason cannot exceed 500 characters.");

        // Rejection errors
        public static readonly Error InvalidRejectionReason = Error.Custom(
            "Organization.InvalidRejectionReason",
            "Rejection reason cannot be empty.");

        public static readonly Error RejectionReasonTooShort = Error.Custom(
            "Organization.RejectionReasonTooShort",
            "Rejection reason must be at least 5 characters long.");

        public static readonly Error RejectionReasonTooLong = Error.Custom(
            "Organization.RejectionReasonTooLong",
            "Rejection reason cannot exceed 500 characters.");

        public static readonly Error CannotRejectApproved = Error.Custom(
            "Organization.CannotRejectApproved",
            "Cannot reject an already approved organization.");

        // Deletion errors
        public static readonly Error InvalidDeletionReason = Error.Custom(
            "Organization.InvalidDeletionReason",
            "Deletion reason cannot be empty.");

        public static readonly Error CannotDeleteApproved = Error.Custom(
            "Organization.CannotDeleteApproved",
            "Cannot delete an approved organization. Deactivate it first.");

        public static readonly Error CannotDeleteWithActiveSubscriptions = Error.Custom(
            "Organization.CannotDeleteWithActiveSubscriptions",
            "Cannot delete an organization with active subscriptions.");

        public static readonly Error CannotDeleteWithPendingInvoices = Error.Custom(
            "Organization.CannotDeleteWithPendingInvoices",
            "Cannot delete an organization with pending invoices.");

        // Settings errors
        public static readonly Error InvalidSettings = Error.Custom(
            "Organization.InvalidSettings",
            "Organization settings are invalid.");

        public static readonly Error CannotModifyApprovedOrganization = Error.Custom(
            "Organization.CannotModifyApprovedOrganization",
            "Cannot modify core properties of an approved organization.");

        // Parent organization errors
        public static readonly Error InvalidParentOrganization = Error.Custom(
            "Organization.InvalidParentOrganization",
            "Parent organization does not exist.");

        public static readonly Error CircularHierarchy = Error.Custom(
            "Organization.CircularHierarchy",
            "Cannot create circular organization hierarchy.");

        public static readonly Error SelfAsParent = Error.Custom(
            "Organization.SelfAsParent",
            "An organization cannot be its own parent.");

        // General errors
        public static readonly Error OrganizationNotFound = Error.Custom(
            "Organization.NotFound",
            "The requested organization does not exist.");

        public static readonly Error InvalidOperation = Error.Custom(
            "Organization.InvalidOperation",
            "The requested operation is invalid for the current organization state.");

        public static readonly Error ConcurrencyConflict = Error.Custom(
            "Organization.ConcurrencyConflict",
            "Organization was modified by another process. Please refresh and try again.");

        public static readonly Error InsufficientPermissions = Error.Custom(
            "Organization.InsufficientPermissions",
            "You do not have permission to perform this action.");

        public static readonly Error DataIntegrityViolation = Error.Custom(
            "Organization.DataIntegrityViolation",
            "Data integrity violation. Please ensure all required fields are valid.");
    }
}
