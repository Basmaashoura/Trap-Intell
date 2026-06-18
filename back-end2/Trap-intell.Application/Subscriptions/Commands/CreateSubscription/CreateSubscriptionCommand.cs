using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    Guid OrganizationId,
    Guid PlanId,
    BillingCycle BillingCycle,
    bool IsTrial,
    int TrialDays,
    bool ActivateImmediately) : IRequest<Result<Guid>>;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.TrialDays)
            .GreaterThan(0)
            .When(x => x.IsTrial)
            .WithMessage("Trial days must be greater than zero for trial subscriptions.");

        RuleFor(x => x.TrialDays)
            .LessThanOrEqualTo(30)
            .When(x => x.IsTrial)
            .WithMessage("Trial days cannot exceed 30 days.");
    }
}
