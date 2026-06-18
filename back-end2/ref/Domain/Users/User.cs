using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Core user entity for authentication and identity.
/// Can be Staff (admin/analyst) or EndUser (monitored user).
/// </summary>
public sealed class User : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// User's email address (used for login).
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Hashed password (BCrypt).
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Type of user account (Staff or EndUser).
    /// </summary>
    public AccountType AccountType { get; private set; }

    /// <summary>
    /// Current status of the user (Active, Inactive, Suspended).
    /// Used for Offboarding Protocol.
    /// </summary>
    public UserStatus Status { get; private set; } = UserStatus.Active;

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// When the user was deactivated (for offboarding tracking).
    /// </summary>
    public DateTime? DeactivatedAt { get; private set; }

    /// <summary>
    /// Navigation property for staff profile (only for Staff users).
    /// </summary>
    public UserProfile? Profile { get; init; }

    /// <summary>
    /// Navigation property for user roles.
    /// </summary>
    public List<UserRole> UserRoles { get; private set; } = [];

    /// <summary>
    /// Navigation property for user sessions.
    /// </summary>
    public List<UserSession> Sessions { get; private set; } = [];

    /// <summary>
    /// Full name helper property.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    private User()
    {
    }

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        AccountType accountType)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            AccountType = accountType,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void UpdateEmail(string email)
    {
        Email = email;
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin(string? ipAddress = null, string? userAgent = null)
    {
        LastLoginAt = DateTime.UtcNow;
        Raise(new UserLoggedInDomainEvent(Id, ipAddress, userAgent));
    }

    /// <summary>
    /// Deactivates the user as part of the Offboarding Protocol.
    /// This invalidates all sessions and prevents login.
    /// </summary>
    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
        {
            return;
        }

        Status = UserStatus.Inactive;
        DeactivatedAt = DateTime.UtcNow;

        // Revoke all active sessions (Immediate Lockout)
        RevokeAllSessions("User deactivated - Offboarding Protocol");

        Raise(new UserDeactivatedDomainEvent(Id));
    }

    /// <summary>
    /// Suspends the user temporarily.
    /// </summary>
    public void Suspend()
    {
        if (Status == UserStatus.Suspended)
        {
            return;
        }

        Status = UserStatus.Suspended;

        // Revoke all active sessions
        RevokeAllSessions("User suspended");

        Raise(new UserSuspendedDomainEvent(Id));
    }

    /// <summary>
    /// Reactivates a suspended or inactive user.
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        Status = UserStatus.Active;
        DeactivatedAt = null;

        Raise(new UserActivatedDomainEvent(Id));
    }

    /// <summary>
    /// Revokes all active sessions for this user.
    /// </summary>
    public void RevokeAllSessions(string reason = "Sessions revoked")
    {
        foreach (UserSession session in Sessions.Where(s => s.IsActive))
        {
            session.Revoke(reason);
        }
    }

    /// <summary>
    /// Creates a new session for this user.
    /// </summary>
    public UserSession CreateSession(
        string tokenHash,
        string? refreshTokenHash,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null)
    {
        var session = UserSession.Create(
            Id,
            tokenHash,
            refreshTokenHash,
            expiresAt,
            ipAddress,
            userAgent,
            deviceId);

        Sessions.Add(session);

        return session;
    }

    /// <summary>
    /// Gets active sessions count.
    /// </summary>
    public int ActiveSessionsCount => Sessions.Count(s => s.IsValid);

    /// <summary>
    /// Checks if user can login.
    /// </summary>
    public bool CanLogin => Status == UserStatus.Active;

    /// <summary>
    /// Checks if user is a staff member.
    /// </summary>
    public bool IsStaff => AccountType == AccountType.Staff;

    /// <summary>
    /// Checks if user is an end user.
    /// </summary>
    public bool IsEndUser => AccountType == AccountType.EndUser;

    /// <summary>
    /// Checks if user has a specific role.
    /// </summary>
    public bool HasRole(string roleName)
    {
        return UserRoles.Exists(ur =>
            ur.Role != null &&
            ur.Role.IsActive &&
            ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if user is a Super Admin.
    /// </summary>
    public bool IsSuperAdmin => HasRole("Super Admin");

    /// <summary>
    /// Checks if user can view sensitive data (based on any of their roles).
    /// </summary>
    public bool CanViewSensitiveData =>
        UserRoles.Exists(ur => ur.Role != null && ur.Role.CanViewSensitiveData);
}
