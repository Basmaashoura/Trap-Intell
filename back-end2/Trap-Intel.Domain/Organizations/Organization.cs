using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations.Events;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Represents a tenant organization using the platform.
    /// Enterprise-grade design with factory methods, validation, and domain events.
    /// </summary>
    public class Organization : AggregateRoot<Guid>
    {
        private readonly List<OrganizationAddress> _addresses = new();

        /// <summary>
        /// Parameterless constructor for EF Core.
        /// </summary>
        private Organization() { }

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private Organization(
            Guid id,
            string name,
            OrganizationType type,
            string industry,
            int size,
            OrganizationDomain domain,
            TaxIdentifier taxId,
            ContactInfo contactInfo,
            string website,
            OrganizationSettings settings,
            Guid? parentOrganizationId = null)
            : base(id)
        {
            Name = name;
            Type = type;
            Industry = industry;
            Size = size;
            Domain = domain;
            TaxId = taxId;
            ContactInfo = contactInfo;
            Website = website;
            Settings = settings;
            ParentOrganizationId = parentOrganizationId;
            Status = OrganizationStatus.PendingApproval;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ApprovedAt = null;
        }

        // Properties
        public string Name { get; private set; } = string.Empty;
        public OrganizationType Type { get; private set; }
        public string Industry { get; private set; } = string.Empty;
        public int Size { get; private set; }
        public OrganizationDomain Domain { get; private set; } = null!;
        public TaxIdentifier TaxId { get; private set; } = null!;
        public ContactInfo ContactInfo { get; private set; } = null!;
        public string Website { get; private set; } = string.Empty;
        public string? Tagline { get; private set; }
        public string? Description { get; private set; }
        public string? SupportEmail { get; private set; }
        public string? SupportPhone { get; private set; }
        public string? HeadquartersLocation { get; private set; }
        public string? LinkedInUrl { get; private set; }
        public string? XUrl { get; private set; }
        public string? LogoUrl { get; private set; }
        public string? LogoPublicId { get; private set; }
        public string? CoverImageUrl { get; private set; }
        public string? CoverImagePublicId { get; private set; }
        public OrganizationSettings Settings { get; private set; } = null!;
        public OrganizationStatus Status { get; private set; }
        public Guid? ParentOrganizationId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? ApprovedAt { get; private set; }
        public string? ApprovalNotes { get; private set; }
        public Guid? ApprovedByUserId { get; private set; }

        public IReadOnlyList<OrganizationAddress> Addresses => _addresses.AsReadOnly();

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new organization with full validation.
        /// </summary>
        public static Result<Organization> Create(
            string name,
            OrganizationType type,
            string industry,
            int size,
            OrganizationDomain domain,
            TaxIdentifier taxId,
            ContactInfo contactInfo,
            string website,
            OrganizationSettings? settings = null,
            Guid? parentOrganizationId = null)
        {
            // Validation
            var validationResult = ValidateOrganizationData(name, industry, size, website);
            if (validationResult.IsFailure)
                return Result.Failure<Organization>(validationResult.Errors[0]);

            var finalSettings = settings ?? new OrganizationSettings();

            var organization = new Organization(
                Guid.NewGuid(),
                name.Trim(),
                type,
                industry.Trim(),
                size,
                domain,
                taxId,
                contactInfo,
                website.Trim(),
                finalSettings,
                parentOrganizationId);

            // Raise domain event
            organization.RaiseDomainEvent(new OrganizationCreatedEvent(
                organization.Id,
                organization.Name,
                organization.Industry,
                DateTime.UtcNow));

            return Result.Success(organization);
        }

        /// <summary>
        /// Factory method to create an organization from existing database data (for reconstruction).
        /// </summary>
        public static Organization Reconstruct(
            Guid id,
            string name,
            OrganizationType type,
            string industry,
            int size,
            OrganizationDomain domain,
            TaxIdentifier taxId,
            ContactInfo contactInfo,
            string website,
            OrganizationSettings settings,
            OrganizationStatus status,
            DateTime createdAt,
            DateTime updatedAt,
            DateTime? approvedAt = null,
            string? approvalNotes = null,
            Guid? approvedByUserId = null,
            Guid? parentOrganizationId = null)
        {
            var organization = new Organization(
                id,
                name,
                type,
                industry,
                size,
                domain,
                taxId,
                contactInfo,
                website,
                settings,
                parentOrganizationId)
            {
                Status = status,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                ApprovedAt = approvedAt,
                ApprovalNotes = approvalNotes,
                ApprovedByUserId = approvedByUserId
            };

            return organization;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Approve the organization (usually by admin).
        /// </summary>
        public Result Approve(Guid approvedByUserId, string? notes = null)
        {
            if (Status == OrganizationStatus.Active)
                return Result.Failure(Error.Custom("Organization.AlreadyApproved", "Organization is already approved."));

            if (Status == OrganizationStatus.Suspended)
                return Result.Failure(Error.Custom("Organization.CannotApproveSuspended", "Cannot approve a suspended organization."));

            Status = OrganizationStatus.Active;
            ApprovedAt = DateTime.UtcNow;
            ApprovedByUserId = approvedByUserId;
            ApprovalNotes = notes;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationApprovedEvent(Id, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Approve the organization with optional notes (overload for service usage).
        /// </summary>
        public Result Approve(string? notes = null)
        {
            if (Status == OrganizationStatus.Active)
                return Result.Failure(Error.Custom("Organization.AlreadyApproved", "Organization is already approved."));

            if (Status == OrganizationStatus.Suspended)
                return Result.Failure(Error.Custom("Organization.CannotApproveSuspended", "Cannot approve a suspended organization."));

            Status = OrganizationStatus.Active;
            ApprovedAt = DateTime.UtcNow;
            ApprovalNotes = notes;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationApprovedEvent(Id, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Reject the organization.
        /// </summary>
        public Result Reject(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(Error.Custom("Organization.InvalidReason", "Rejection reason cannot be empty."));

            Status = OrganizationStatus.Inactive;
            ApprovalNotes = reason;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationRejectedEvent(Id, reason, DateTime.UtcNow));

            return Result.Success();
        }

        public Result AddAddress(Address address, AddressType addressType)
        {
            if (address is null)
                return Result.Failure(OrganizationErrors.InvalidAddress);

            if (!Settings.AllowMultipleAddresses && _addresses.Count > 0)
                return Result.Failure(OrganizationErrors.MultipleAddressesNotAllowed);

            var organizationAddress = new OrganizationAddress(Id, address, addressType);
            _addresses.Add(organizationAddress);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new AddressAddedEvent(
                Id,
                address.Street,
                address.City,
                address.State,
                address.PostalCode,
                address.Country,
                addressType.ToString(),
                DateTime.UtcNow));

            return Result.Success();
        }

        public Result RemoveAddress(Address address)
        {
            if (address is null)
                return Result.Failure(OrganizationErrors.InvalidAddress);

            var toRemove = _addresses.Find(a => a.Address == address);
            if (toRemove is null)
                return Result.Failure(OrganizationErrors.AddressNotFound);

            _addresses.Remove(toRemove);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new AddressRemovedEvent(
                Id,
                address.Street,
                address.City,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update organization information with validation.
        /// </summary>
        public Result UpdateOrganizationInfo(
            string name,
            OrganizationType type,
            string industry,
            int size,
            ContactInfo contactInfo)
        {
            var validationResult = ValidateOrganizationData(name, industry, size, Website);
            if (validationResult.IsFailure)
                return Result.Failure(validationResult.Errors[0]);

            var changedFields = new List<string>();

            if (Name != name.Trim())
                changedFields.Add(nameof(Name));
            if (Type != type)
                changedFields.Add(nameof(Type));
            if (Industry != industry.Trim())
                changedFields.Add(nameof(Industry));
            if (Size != size)
                changedFields.Add(nameof(Size));
            if (ContactInfo != contactInfo)
                changedFields.Add(nameof(ContactInfo));

            Name = name.Trim();
            Type = type;
            Industry = industry.Trim();
            Size = size;
            ContactInfo = contactInfo;
            UpdatedAt = DateTime.UtcNow;

            if (changedFields.Count > 0)
            {
                RaiseDomainEvent(new OrganizationInfoUpdatedEvent(
                    Id,
                    string.Join(", ", changedFields),
                    DateTime.UtcNow));
            }

            return Result.Success();
        }

        public void Activate()
        {
            if (Status == OrganizationStatus.Active)
                return;

            Status = OrganizationStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationActivatedEvent(Id, DateTime.UtcNow));
        }

        public void Suspend()
        {
            if (Status == OrganizationStatus.Suspended)
                return;

            Status = OrganizationStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationSuspendedEvent(Id, DateTime.UtcNow));
        }

        public Result Delete(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(Error.Custom("Organization.InvalidDeletionReason", "Deletion reason cannot be empty."));

            Status = OrganizationStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationDeletedEvent(Id, reason, DateTime.UtcNow));

            return Result.Success();
        }

        public Result UpdateSettings(OrganizationSettings newSettings)
        {
            if (newSettings is null)
                return Result.Failure(OrganizationErrors.InvalidSettings);

            Settings = newSettings;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        public Result UpdateProfile(
            string? tagline,
            string? description,
            string? supportEmail,
            string? supportPhone,
            string? headquartersLocation,
            string? linkedInUrl,
            string? xUrl)
        {
            if (!IsValidOptionalEmail(supportEmail))
                return Result.Failure(Error.Custom("Organization.InvalidSupportEmail", "Support email is invalid."));

            if (!IsValidOptionalUrl(linkedInUrl) || !IsValidOptionalUrl(xUrl))
                return Result.Failure(Error.Custom("Organization.InvalidProfileUrl", "One or more organization profile links are invalid."));

            Tagline = NormalizeOptional(tagline, 250);
            Description = NormalizeOptional(description, 4000);
            SupportEmail = NormalizeOptional(supportEmail, 254);
            SupportPhone = NormalizeOptional(supportPhone, 30);
            HeadquartersLocation = NormalizeOptional(headquartersLocation, 250);
            LinkedInUrl = NormalizeOptional(linkedInUrl, 500);
            XUrl = NormalizeOptional(xUrl, 500);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrganizationInfoUpdatedEvent(
                Id,
                "Profile",
                DateTime.UtcNow));

            return Result.Success();
        }

        public Result SetLogo(string? logoUrl, string? logoPublicId)
        {
            if (!IsValidOptionalUrl(logoUrl))
                return Result.Failure(Error.Custom("Organization.InvalidLogoUrl", "Logo URL is invalid."));

            LogoUrl = NormalizeOptional(logoUrl, 500);
            LogoPublicId = NormalizeOptional(logoPublicId, 255);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        public Result SetCoverImage(string? coverImageUrl, string? coverImagePublicId)
        {
            if (!IsValidOptionalUrl(coverImageUrl))
                return Result.Failure(Error.Custom("Organization.InvalidCoverImageUrl", "Cover image URL is invalid."));

            CoverImageUrl = NormalizeOptional(coverImageUrl, 500);
            CoverImagePublicId = NormalizeOptional(coverImagePublicId, 255);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        #endregion

        #region Hierarchical Organization Support

        /// <summary>
        /// Check if this is a root organization (no parent).
        /// </summary>
        public bool IsRootOrganization => ParentOrganizationId is null;

        /// <summary>
        /// Check if this is a child organization (has parent).
        /// </summary>
        public bool IsChildOrganization => ParentOrganizationId is not null;

        /// <summary>
        /// Check if this organization can create a new subscription.
        /// </summary>
        public bool CanCreateNewSubscription(int currentSubscriptionCount, int maxAllowed = 10)
        {
            return currentSubscriptionCount < maxAllowed;
        }

        #endregion

        #region Private Validation Methods

        private static Result ValidateOrganizationData(string name, string industry, int size, string website)
        {
            var errors = new List<Error>();

            if (string.IsNullOrWhiteSpace(name))
                errors.Add(Error.Custom("Organization.InvalidName", "Organization name cannot be empty."));

            if (size <= 0)
                errors.Add(Error.Custom("Organization.InvalidSize", "Organization size must be greater than zero."));

            if (string.IsNullOrWhiteSpace(industry))
                errors.Add(Error.Custom("Organization.InvalidIndustry", "Organization industry cannot be empty."));

            if (string.IsNullOrWhiteSpace(website))
                errors.Add(Error.Custom("Organization.InvalidWebsite", "Organization website cannot be empty."));

            if (errors.Count > 0)
                return Result.Failure(errors);

            return Result.Success();
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static bool IsValidOptionalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        private static bool IsValidOptionalEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            try
            {
                _ = new System.Net.Mail.MailAddress(value.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
