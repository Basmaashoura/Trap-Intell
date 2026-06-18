namespace Trap_Intel.Domain.Invitations.Enums;

/// <summary>
/// Status of an organization invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>Invitation is pending response.</summary>
    Pending = 0,
    
    /// <summary>Invitation was accepted.</summary>
    Accepted = 1,
    
    /// <summary>Invitation was declined by invitee.</summary>
    Declined = 2,
    
    /// <summary>Invitation was revoked by organization.</summary>
    Revoked = 3,
    
    /// <summary>Invitation has expired.</summary>
    Expired = 4
}

/// <summary>
/// Type of invitation (for future extensibility).
/// </summary>
public enum InvitationType
{
    /// <summary>Standard team member invitation.</summary>
    TeamMember = 0,
    
    /// <summary>Guest access invitation (temporary).</summary>
    Guest = 1,
    
    /// <summary>External collaborator invitation.</summary>
    Collaborator = 2,
    
    /// <summary>Admin transfer invitation.</summary>
    AdminTransfer = 3
}
