using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Plans.ValueObjects;

namespace Trap_Intel.Application.Plans.Commands.CreatePlan;

internal sealed class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Result<Guid>>
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var nameExists = await _planRepository.NameExistsAsync(normalizedName, cancellationToken);
        if (nameExists)
        {
            return Result.Failure<Guid>(PlanErrors.DuplicateName);
        }

        var supportTier = new SupportTierConfig(
            request.SupportLevel,
            request.SupportResponseTimeMinutes,
            request.IncludesDedicatedManager);

        var certifications = (request.RequiredCertifications ?? Array.Empty<string>())
            .Where(certification => !string.IsNullOrWhiteSpace(certification))
            .Select(certification => certification.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var compliance = new ComplianceConfig(
            request.ComplianceLevel,
            certifications,
            request.ComplianceAuditingIncluded);

        var createPlanResult = Plan.Create(
            normalizedName,
            request.Description.Trim(),
            request.Type,
            supportTier,
            compliance,
            request.CustomizationLevel);

        if (createPlanResult.IsFailure)
        {
            return Result.Failure<Guid>(createPlanResult.Errors);
        }

        var plan = createPlanResult.Value;

        var addPricingResult = plan.AddPricing(
            request.BillingCycle,
            new PlanPrice(
                request.PriceAmount,
                request.Currency.Trim().ToUpperInvariant(),
                request.SetupFee));

        if (addPricingResult.IsFailure)
        {
            return Result.Failure<Guid>(addPricingResult.Errors);
        }

        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Id);
    }
}
