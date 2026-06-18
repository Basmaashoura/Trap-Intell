using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Organizations.Commands.UpdateOrganizationStatus;

internal sealed class UpdateOrganizationStatusCommandHandler : IRequestHandler<UpdateOrganizationStatusCommand, Result<OrganizationStatusDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrganizationStatusCommandHandler> _logger;

    public UpdateOrganizationStatusCommandHandler(
        IOrganizationRepository organizationRepository,
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrganizationStatusCommandHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrganizationStatusDto>> Handle(UpdateOrganizationStatusCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result.Failure<OrganizationStatusDto>(OrganizationErrors.OrganizationNotFound);
        }

        if (organization.Status == request.TargetStatus)
        {
            return Result.Success(MapToDto(organization));
        }

        var transitionRule = new OrganizationStatusTransitionRule(organization.Status, request.TargetStatus);
        if (!transitionRule.IsSatisfied())
        {
            return Result.Failure<OrganizationStatusDto>(transitionRule.Error);
        }

        Result transitionResult;
        if (request.TargetStatus == OrganizationStatus.Active)
        {
            transitionResult = await ApplyActiveTransitionAsync(organization, request, cancellationToken);
        }
        else
        {
            transitionResult = request.TargetStatus switch
            {
                OrganizationStatus.Suspended => ApplySuspendedTransition(organization),
                OrganizationStatus.Inactive => ApplyInactiveTransition(organization, request.Reason),
                _ => Result.Failure(Error.Custom(
                    "Organization.UnsupportedStatus",
                    "Unsupported organization status transition target."))
            };
        }

        if (transitionResult.IsFailure)
        {
            return Result.Failure<OrganizationStatusDto>(transitionResult.Errors);
        }

        await _organizationRepository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(organization));
    }

    private async Task<Result> ApplyActiveTransitionAsync(
        Organization organization,
        UpdateOrganizationStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (organization.Status == OrganizationStatus.PendingApproval)
        {
            var approveResult = organization.Approve(request.ChangedByUserId, request.Reason);
            if (approveResult.IsFailure)
            {
                return approveResult;
            }

            if (!organization.Settings.EnableBilling)
            {
                return Result.Success();
            }

            return await BootstrapTrialSubscriptionAsync(organization, cancellationToken);
        }

        organization.Activate();
        return Result.Success();
    }

    private async Task<Result> BootstrapTrialSubscriptionAsync(Organization organization, CancellationToken cancellationToken)
    {
        var existingSubscription = await _subscriptionRepository.GetByOrganizationIdAsync(organization.Id, cancellationToken);
        if (existingSubscription is not null &&
            (existingSubscription.Status == SubscriptionStatus.Active || existingSubscription.Status == SubscriptionStatus.Trial))
        {
            return Result.Success();
        }

        var trialPlan = (await _planRepository.GetByTypeAsync(PlanType.Trial, cancellationToken))
            .FirstOrDefault(plan => plan.IsActive);

        if (trialPlan is null)
        {
            return Result.Failure(Error.Custom(
                "Organization.BillingBootstrapPlanNotFound",
                "No active trial plan is available for organization billing bootstrap."));
        }

        var creationService = new CreateSubscriptionService(_planRepository, _subscriptionRepository);
        var createResult = await creationService.CreateTrialAsync(
            organization.Id,
            trialPlan.Id,
            trialDays: 14,
            cancellationToken: cancellationToken);

        if (createResult.IsFailure)
        {
            _logger.LogWarning(
                "Organization billing bootstrap failed for organization {OrganizationId}. Error={ErrorCode}",
                organization.Id,
                createResult.Errors.FirstOrDefault()?.Code);

            return Result.Failure(createResult.Errors);
        }

        return Result.Success();
    }

    private static Result ApplySuspendedTransition(Organization organization)
    {
        organization.Suspend();
        return Result.Success();
    }

    private static Result ApplyInactiveTransition(Organization organization, string? reason)
    {
        var finalReason = string.IsNullOrWhiteSpace(reason)
            ? "Organization deactivated by administrator."
            : reason.Trim();

        return organization.Delete(finalReason);
    }

    private static OrganizationStatusDto MapToDto(Organization organization)
    {
        return new OrganizationStatusDto(
            organization.Id,
            organization.Name,
            organization.Status.ToString(),
            organization.UpdatedAt,
            organization.ApprovedAt,
            organization.ApprovalNotes);
    }
}
