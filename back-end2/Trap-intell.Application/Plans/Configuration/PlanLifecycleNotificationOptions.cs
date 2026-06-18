namespace Trap_Intel.Application.Plans.Configuration;

public sealed class PlanLifecycleNotificationOptions
{
    public const string SectionName = "Notifications:PlanLifecycle";

    public bool IncludeOrganizationAdmins { get; set; } = true;
}