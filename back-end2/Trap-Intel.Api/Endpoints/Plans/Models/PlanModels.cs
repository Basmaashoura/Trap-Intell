namespace Trap_Intel.Api.Endpoints.Plans.Models;

public sealed record CreatePlanRequest(
    string Name,
    string Description,
    string Type,
    string SupportLevel,
    int SupportResponseTimeMinutes,
    bool IncludesDedicatedManager,
    string ComplianceLevel,
    IReadOnlyCollection<string>? RequiredCertifications,
    bool ComplianceAuditingIncluded,
    string CustomizationLevel,
    string BillingCycle,
    decimal PriceAmount,
    string Currency,
    decimal SetupFee);
