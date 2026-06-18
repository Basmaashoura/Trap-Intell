using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Plans.Queries.GetPlanById;

internal sealed class GetPlanByIdQueryHandler : IRequestHandler<GetPlanByIdQuery, Result<PlanDetailDto>>
{
    private readonly IPlanRepository _planRepository;

    public GetPlanByIdQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<PlanDetailDto>> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<PlanDetailDto>(PlanErrors.PlanNotFound);
        }

        var supportTier = new PlanSupportTierDto(
            plan.SupportTier.Level,
            plan.SupportTier.ResponseTimeMinutes,
            plan.SupportTier.IncludesDedicatedManager);

        var compliance = new PlanComplianceDto(
            plan.ComplianceConfig.Level,
            plan.ComplianceConfig.RequiredCertifications,
            plan.ComplianceConfig.AuditingIncluded);

        var pricing = plan.Pricing
            .OrderBy(x => x.Key)
            .Select(x => new PlanPricingDto(
                x.Key,
                x.Value.Amount,
                x.Value.Currency,
                x.Value.SetupFee))
            .ToList();

        var features = plan.Features
            .OrderBy(feature => feature.SortOrder)
            .ThenBy(feature => feature.Code)
            .Select(feature => new PlanFeatureDto(
                feature.Code,
                feature.Name,
                feature.Description,
                feature.Category,
                feature.IsEnabled,
                feature.LimitValue,
                feature.LimitUnit,
                feature.IsPremium,
                feature.SortOrder))
            .ToList();

        PlanQuotaDto? quota = null;
        if (plan.QuotaDefinition is not null)
        {
            quota = new PlanQuotaDto(
                plan.QuotaDefinition.MaxHoneypots,
                plan.QuotaDefinition.MaxStorageGb,
                plan.QuotaDefinition.MaxMonthlyApiCalls,
                plan.QuotaDefinition.MaxUsers,
                plan.QuotaDefinition.MaxAttackEventsRetained,
                plan.QuotaDefinition.DataRetentionDays,
                plan.QuotaDefinition.MaxMonthlyReports,
                plan.QuotaDefinition.MaxWebhooks,
                plan.QuotaDefinition.MaxApiKeys,
                plan.QuotaDefinition.HardLimitEnforced,
                plan.QuotaDefinition.OverageHoneypotRate,
                plan.QuotaDefinition.OverageStorageRatePerGb,
                plan.QuotaDefinition.OverageApiCallRatePer1000);
        }

        var dto = new PlanDetailDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Type,
            plan.CustomizationLevel,
            plan.IsActive,
            supportTier,
            compliance,
            pricing,
            features,
            quota,
            plan.CreatedAt,
            plan.UpdatedAt);

        return Result.Success(dto);
    }
}
