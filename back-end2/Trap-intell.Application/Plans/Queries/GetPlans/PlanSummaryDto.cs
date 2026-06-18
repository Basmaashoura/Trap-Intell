using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Plans.Queries.GetPlans;

public sealed record PlanSummaryDto(
    Guid Id,
    string Name,
    string Description,
    PlanType Type,
    bool IsActive,
    decimal? MonthlyPrice,
    string Currency);
