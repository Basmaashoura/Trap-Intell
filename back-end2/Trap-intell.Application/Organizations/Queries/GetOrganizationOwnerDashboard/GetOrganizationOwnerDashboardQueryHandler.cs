using MediatR;
using Trap_Intel.Application.Abstractions.Alerts;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationOwnerDashboard;

internal sealed class GetOrganizationOwnerDashboardQueryHandler : IRequestHandler<GetOrganizationOwnerDashboardQuery, Result<OrganizationOwnerDashboardDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IAlertQueryService _alertQueryService;
    private readonly IAuditDashboardQueryService _auditDashboardQueryService;

    public GetOrganizationOwnerDashboardQueryHandler(
        IOrganizationRepository organizationRepository,
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IAlertQueryService alertQueryService,
        IAuditDashboardQueryService auditDashboardQueryService)
    {
        _organizationRepository = organizationRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _alertQueryService = alertQueryService;
        _auditDashboardQueryService = auditDashboardQueryService;
    }

    public async Task<Result<OrganizationOwnerDashboardDto>> Handle(GetOrganizationOwnerDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<OrganizationOwnerDashboardDto>(
                Error.Custom("Organization.InvalidId", "Organization ID cannot be empty."));
        }

        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure<OrganizationOwnerDashboardDto>(OrganizationErrors.OrganizationNotFound);
        }

        var lastNDays = request.LastNDays < 1 ? 30 : Math.Min(request.LastNDays, 365);

        var alertStats = await _alertQueryService.GetDashboardStatisticsAsync(request.OrganizationId, lastNDays, cancellationToken);
        var auditStats = await _auditDashboardQueryService.GetDashboardStatisticsAsync(request.OrganizationId, lastNDays, cancellationToken);

        var subscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);

        OrganizationOwnerSubscriptionSummaryDto? subscriptionDto = null;
        OrganizationOwnerQuotaSummaryDto? quotaDto = null;
        var hasSubscription = subscription is not null;

        if (subscription is not null)
        {
            var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
            var quotaUsage = subscription.GetQuotaUsageSummary();
            var canAddHoneypotResult = subscription.CanAddHoneypot();

            subscriptionDto = new OrganizationOwnerSubscriptionSummaryDto(
                subscription.Id,
                subscription.PlanId,
                plan?.Name,
                plan?.Type.ToString(),
                subscription.Status.ToString(),
                subscription.BillingCycle.ToString(),
                subscription.BillingInfo.TotalBilled,
                subscription.BillingInfo.DiscountApplied,
                subscription.Period.StartDate,
                subscription.Period.EndDate,
                subscription.Period.RenewalDate,
                subscription.IsAutoRenew,
                subscription.UpdatedAt);

            quotaDto = new OrganizationOwnerQuotaSummaryDto(
                quotaUsage.CurrentHoneypots,
                quotaUsage.MaxHoneypots,
                quotaUsage.HoneypotUsagePercent,
                quotaUsage.CurrentStorageGb,
                quotaUsage.MaxStorageGb,
                quotaUsage.StorageUsagePercent,
                quotaUsage.CurrentApiCalls,
                quotaUsage.MaxApiCalls,
                quotaUsage.ApiCallsUsagePercent,
                quotaUsage.IsApproachingLimit,
                quotaUsage.IsOverLimit,
                subscription.CalculateOverageCharges(),
                subscription.HasOverages(),
                canAddHoneypotResult.IsSuccess && canAddHoneypotResult.Value,
                subscription.IsCancellationScheduled);
        }

        var alertsDto = new OrganizationOwnerAlertSummaryDto(
            alertStats.TotalActiveAlerts,
            alertStats.UnacknowledgedAlerts,
            alertStats.CriticalUnresolvedAlerts,
            alertStats.EscalatedAlerts,
            alertStats.FalsePositivesLastNDays,
            alertStats.AlertsByType
                .Select(x => new OrganizationOwnerTrendItemDto(x.Category, x.Count))
                .ToList(),
            alertStats.AlertsBySeverity
                .Select(x => new OrganizationOwnerTrendItemDto(x.Category, x.Count))
                .ToList());

        var auditingDto = new OrganizationOwnerAuditSummaryDto(
            auditStats.TotalEvents,
            auditStats.UnacknowledgedCriticalEvents,
            auditStats.HighSeverityEvents,
            auditStats.TopResourceTypes
                .Select(x => new OrganizationOwnerAuditResourceItemDto(x.ResourceType.ToString(), x.Count))
                .ToList(),
            auditStats.RecentCriticalEvents
                .Select(x => new OrganizationOwnerRecentAuditEventDto(
                    x.Id,
                    x.Action.ToString(),
                    x.ResourceType.ToString(),
                    x.Timestamp,
                    x.Reason))
                .ToList());

        var dto = new OrganizationOwnerDashboardDto(
            organization.Id,
            organization.Name,
            organization.Status.ToString(),
            hasSubscription,
            subscriptionDto,
            quotaDto,
            alertsDto,
            auditingDto,
            DateTime.UtcNow);

        return Result.Success(dto);
    }
}
