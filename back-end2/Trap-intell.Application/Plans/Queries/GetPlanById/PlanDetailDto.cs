using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Plans.ValueObjects;

namespace Trap_Intel.Application.Plans.Queries.GetPlanById;

public sealed record PlanSupportTierDto(
    SupportLevel Level,
    int ResponseTimeMinutes,
    bool IncludesDedicatedManager);

public sealed record PlanComplianceDto(
    ComplianceLevel Level,
    IReadOnlyList<string> RequiredCertifications,
    bool AuditingIncluded);

public sealed record PlanPricingDto(
    BillingCycle BillingCycle,
    decimal Amount,
    string Currency,
    decimal SetupFee);

public sealed record PlanFeatureDto(
    string Code,
    string Name,
    string Description,
    FeatureCategory Category,
    bool IsEnabled,
    int? LimitValue,
    string? LimitUnit,
    bool IsPremium,
    int SortOrder);

public sealed record PlanQuotaDto(
    int MaxHoneypots,
    decimal MaxStorageGb,
    int MaxMonthlyApiCalls,
    int MaxUsers,
    int MaxAttackEventsRetained,
    int DataRetentionDays,
    int MaxMonthlyReports,
    int MaxWebhooks,
    int MaxApiKeys,
    bool HardLimitEnforced,
    decimal OverageHoneypotRate,
    decimal OverageStorageRatePerGb,
    decimal OverageApiCallRatePer1000);

public sealed record PlanDetailDto(
    Guid Id,
    string Name,
    string Description,
    PlanType Type,
    CustomizationLevel CustomizationLevel,
    bool IsActive,
    PlanSupportTierDto SupportTier,
    PlanComplianceDto Compliance,
    IReadOnlyList<PlanPricingDto> Pricing,
    IReadOnlyList<PlanFeatureDto> Features,
    PlanQuotaDto? Quota,
    DateTime CreatedAt,
    DateTime UpdatedAt);
