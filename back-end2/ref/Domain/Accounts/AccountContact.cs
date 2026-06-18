using Domain.Users;
using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Links an employee (User) to an Organization (Account).
/// An organization can have multiple employees, one must be the primary contact.
/// Each employee has specific permissions controlled by the primary contact or admin.
/// </summary>
public sealed class AccountContact : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The organization this employee belongs to.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// The user who is the employee.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Whether this is the primary contact for the organization.
    /// Primary contact receives important notifications and reports.
    /// </summary>
    public bool IsPrimaryContact { get; private set; }

    /// <summary>
    /// The employee's role/position at the organization.
    /// Example: "Team Lead", "HR Manager", "Software Engineer"
    /// </summary>
    public string? Role { get; private set; }

    /// <summary>
    /// Whether this employee is a decision maker.
    /// Used for subscription and account management communications.
    /// </summary>
    public bool IsDecisionMaker { get; private set; }

    /// <summary>
    /// What this contact can see and do.
    /// Controlled by primary contact or admin.
    /// </summary>
    public ContactPermissions Permissions { get; private set; } = null!;

    /// <summary>
    /// Who invited this contact (User ID).
    /// Null if added by admin.
    /// </summary>
    public Guid? InvitedBy { get; private set; }

    /// <summary>
    /// When the invitation was sent.
    /// </summary>
    public DateTime? InvitedAt { get; private set; }

    /// <summary>
    /// When the invitation expires.
    /// Null means never expires.
    /// </summary>
    public DateTime? InviteExpiresAt { get; private set; }

    /// <summary>
    /// Secure token for accepting the invitation.
    /// Hashed for security - never store plain text.
    /// </summary>
    public string? InvitationTokenHash { get; private set; }

    /// <summary>
    /// Whether the invitation has been accepted.
    /// </summary>
    public bool IsInviteAccepted { get; private set; }

    /// <summary>
    /// When the invitation was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>
    /// Whether this contact is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When this contact was added to the account.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this contact was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Navigation property to Account.
    /// </summary>
    public Account? Account { get; init; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; init; }

    private AccountContact()
    {
    }

    /// <summary>
    /// Creates a new contact added directly by admin (no invitation).
    /// </summary>
    public static AccountContact CreateDirect(
        Guid accountId,
        Guid userId,
        bool isPrimaryContact,
        string? role = null,
        bool isDecisionMaker = false)
    {
        ContactPermissions permissions = isPrimaryContact || isDecisionMaker
            ? ContactPermissions.CreateFull()
            : ContactPermissions.CreateDefault();

        var contact = new AccountContact
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            IsPrimaryContact = isPrimaryContact,
            Role = role,
            IsDecisionMaker = isDecisionMaker,
            Permissions = permissions,
            IsInviteAccepted = true, // Direct add = already accepted
            AcceptedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        contact.Raise(new AccountContactAddedDomainEvent(accountId, userId, isPrimaryContact));

        return contact;
    }

    /// <summary>
    /// Creates an invitation for a new contact.
    /// </summary>
    public static AccountContact CreateInvitation(
        Guid accountId,
        Guid userId,
        Guid invitedBy,
        string? role,
        ContactPermissions permissions,
        int expirationDays = 7)
    {
        var contact = new AccountContact
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            IsPrimaryContact = false,
            Role = role,
            IsDecisionMaker = false,
            Permissions = permissions,
            InvitedBy = invitedBy,
            InvitedAt = DateTime.UtcNow,
            InviteExpiresAt = expirationDays > 0 ? DateTime.UtcNow.AddDays(expirationDays) : null,
            IsInviteAccepted = false,
            IsActive = false, // Not active until accepted
            CreatedAt = DateTime.UtcNow
        };

        return contact;
    }

    /// <summary>
    /// Accepts the invitation.
    /// </summary>
    public Result AcceptInvitation()
    {
        if (IsInviteAccepted)
        {
            return Result.Failure(AccountContactErrors.InviteAlreadyAccepted);
        }

        if (InviteExpiresAt.HasValue && DateTime.UtcNow > InviteExpiresAt.Value)
        {
            return Result.Failure(AccountSettingsErrors.InviteExpired);
        }

        IsInviteAccepted = true;
        AcceptedAt = DateTime.UtcNow;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        Raise(new AccountContactAddedDomainEvent(AccountId, UserId, false));

        return Result.Success();
    }

    public void SetAsPrimary()
    {
        IsPrimaryContact = true;
        Permissions = ContactPermissions.CreateFull();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAsPrimary()
    {
        IsPrimaryContact = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string? role, bool isDecisionMaker)
    {
        Role = role;
        IsDecisionMaker = isDecisionMaker;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the contact's permissions.
    /// </summary>
    public void UpdatePermissions(ContactPermissions permissions)
    {
        Permissions = permissions;
        UpdatedAt = DateTime.UtcNow;
        Raise(new AccountContactPermissionsUpdatedDomainEvent(AccountId, UserId));
    }


    /// <summary>
    /// Deactivates this contact (soft delete).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Raise(new AccountContactRemovedDomainEvent(AccountId, UserId));
    }

    /// <summary>
    /// Reactivates this contact.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this contact has a specific permission.
    /// </summary>
    public bool HasPermission(Func<ContactPermissions, bool> permissionCheck)
    {
        if (!IsActive || !IsInviteAccepted)
        {
            return false;
        }

        // Primary contacts have full access
        if (IsPrimaryContact)
        {
            return true;
        }

        return permissionCheck(Permissions);
    }

    /// <summary>
    /// Sets the invitation token (hashed).
    /// </summary>
    public void SetInvitationToken(string tokenHash)
    {
        InvitationTokenHash = tokenHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates an invitation token.
    /// </summary>
    public bool ValidateInvitationToken(string tokenHash)
    {
        if (string.IsNullOrEmpty(InvitationTokenHash))
        {
            return false;
        }

        if (InviteExpiresAt.HasValue && DateTime.UtcNow > InviteExpiresAt.Value)
        {
            return false;
        }

        return InvitationTokenHash == tokenHash;
    }

    /// <summary>
    /// Clears the invitation token after acceptance.
    /// </summary>
    public void ClearInvitationToken()
    {
        InvitationTokenHash = null;
    }

    /// <summary>
    /// Checks if invitation has expired.
    /// </summary>
    public bool IsInvitationExpired => InviteExpiresAt.HasValue && DateTime.UtcNow > InviteExpiresAt.Value;

    /// <summary>
    /// Refreshes the invitation with new expiration.
    /// </summary>
    public void RefreshInvitation(string tokenHash, int expirationDays = 7)
    {
        InvitationTokenHash = tokenHash;
        InvitedAt = DateTime.UtcNow;
        InviteExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels/revokes the invitation.
    /// </summary>
    public void CancelInvitation()
    {
        InvitationTokenHash = null;
        InviteExpiresAt = DateTime.UtcNow; // Expired immediately
        UpdatedAt = DateTime.UtcNow;
    }
}
