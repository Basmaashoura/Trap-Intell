namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Enums specific to the Plans domain.
    /// </summary>
    
    public enum PlanType
    {
        Free = 0,
        Paid = 1,
        Trial = 2,
        Custom = 3
    }

    public enum BillingCycle
    {
        Monthly = 0,
        Quarterly = 1,
        Annually = 2,
        OneTime = 3
    }

    public enum SupportLevel
    {
        Basic = 0,
        Priority = 1,
        Dedicated = 2,
        None = 3
    }

    public enum ComplianceLevel
    {
        None = 0,
        GDPR = 1,
        HIPAA = 2,
        SOC2 = 3,
        ISO27001 = 4,
        Custom = 5
    }

    public enum CustomizationLevel
    {
        None = 0,
        Basic = 1,
        Advanced = 2,
        Enterprise = 3
    }
}
