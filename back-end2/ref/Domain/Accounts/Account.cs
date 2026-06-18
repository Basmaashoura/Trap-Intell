using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Represents an organization/company that subscribes to the platform.
/// An organization adds employees who are monitored for stress detection.
/// Flow: Organization → Subscription (Plan) → Employees (AccountContact → User)
/// </summary>
public sealed class Account : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Organization/Company name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Organization industry or business type.
    /// </summary>
    public string? Industry { get; private set; }

    /// <summary>
    /// Organization website URL.
    /// </summary>
    public string? Website { get; private set; }

    /// <summary>
    /// Primary phone number for the organization.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Organization address.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// Internal notes about this organization (visible to staff only).
    /// </summary>
    public string? InternalNotes { get; private set; }

    /// <summary>
    /// When the account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the account was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Whether this account is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property for employees linked to this organization.
    /// </summary>
    public List<AccountContact> Contacts { get; private set; } = [];

    /// <summary>
    /// Navigation property for account settings.
    /// </summary>
    public AccountSettings? Settings { get; init; }

    /// <summary>
    /// Navigation property for subscriptions.
    /// </summary>
    public List<Domain.Subscriptions.Subscription> Subscriptions { get; private set; } = [];

    private Account()
    {
    }

    public static Account Create(
        string name,
        string? industry = null,
        string? website = null,
        string? phone = null,
        string? address = null,
        string? taxNumber = null)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            Industry = industry,
            Website = website,
            Phone = phone,
            Address = address,
            TaxNumber = taxNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        account.Raise(new AccountCreatedDomainEvent(account.Id));

        return account;
    }

    public void Update(
        string name,
        string? industry,
        string? website,
        string? phone,
        string? address,
        string? taxNumber)
    {
        Name = name;
        Industry = industry;
        Website = website;
        Phone = phone;
        Address = address;
        TaxNumber = taxNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Raise(new AccountDeactivatedDomainEvent(Id));
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInternalNotes(string? notes)
    {
        InternalNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the primary contact for this organization.
    /// </summary>
    public AccountContact? GetPrimaryContact()
    {
        return Contacts.Find(c => c.IsPrimaryContact && c.IsActive);
    }

    /// <summary>
    /// Gets all active employees for this organization.
    /// </summary>
    public IReadOnlyList<AccountContact> GetActiveContacts()
    {
        return Contacts.Where(c => c.IsActive && c.IsInviteAccepted).ToList();
    }

    /// <summary>
    /// Checks if a user is an employee of this organization.
    /// </summary>
    public bool HasContact(Guid userId)
    {
        return Contacts.Exists(c => c.UserId == userId && c.IsActive);
    }

    /// <summary>
    /// Gets the employee count (active and accepted only).
    /// </summary>
    public int ActiveContactCount => Contacts.Count(c => c.IsActive && c.IsInviteAccepted);

    /// <summary>
    /// Gets the active subscription for this organization.
    /// </summary>
    public Domain.Subscriptions.Subscription? GetActiveSubscription()
    {
        return Subscriptions.Find(s => s.Status == Domain.Subscriptions.SubscriptionStatus.Active);
    }
}
