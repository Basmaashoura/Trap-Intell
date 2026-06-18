using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Plans.Commands.CreatePlan;

public sealed record CreatePlanCommand(
    string Name,
    string Description,
    PlanType Type,
    SupportLevel SupportLevel,
    int SupportResponseTimeMinutes,
    bool IncludesDedicatedManager,
    ComplianceLevel ComplianceLevel,
    IReadOnlyCollection<string> RequiredCertifications,
    bool ComplianceAuditingIncluded,
    CustomizationLevel CustomizationLevel,
    BillingCycle BillingCycle,
    decimal PriceAmount,
    string Currency,
    decimal SetupFee
) : IRequest<Result<Guid>>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.SupportResponseTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SetupFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0)
            .When(x => x.Type == PlanType.Paid)
            .WithMessage("Paid plans must have a price greater than zero.");
    }
}
