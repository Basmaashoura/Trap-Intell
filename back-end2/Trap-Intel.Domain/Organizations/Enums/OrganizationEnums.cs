namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Enums specific to the Organization domain.
    /// </summary>
    
    public enum OrganizationType
    {
        SMB = 0,
        Educational = 1,
        NGO = 2,
        Government = 3,
        Enterprise = 4,
        Startup = 5,
        Other = 6
    }

    public enum OrganizationStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2,
        PendingApproval = 3
    }

    public enum AddressType
    {
        Billing = 0,
        Headquarters = 1,
        Branch = 2,
        Shipping = 3,
        Other = 4
    }
}
