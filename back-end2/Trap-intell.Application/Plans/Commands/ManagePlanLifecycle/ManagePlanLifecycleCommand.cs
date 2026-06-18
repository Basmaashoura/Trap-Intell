using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Plans.Commands.ManagePlanLifecycle;

public enum PlanLifecycleAction
{
    Activate = 1,
    Deactivate = 2
}

public sealed record ManagePlanLifecycleCommand(
    Guid PlanId,
    PlanLifecycleAction Action) : IRequest<Result>;

public sealed class ManagePlanLifecycleCommandValidator : AbstractValidator<ManagePlanLifecycleCommand>
{
    public ManagePlanLifecycleCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Action)
            .Must(action => action is PlanLifecycleAction.Activate or PlanLifecycleAction.Deactivate)
            .WithMessage("Action must be Activate or Deactivate.");
    }
}
