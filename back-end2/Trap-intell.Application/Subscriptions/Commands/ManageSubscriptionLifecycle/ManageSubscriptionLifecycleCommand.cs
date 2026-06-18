using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;

public enum SubscriptionLifecycleAction
{
    Activate = 1,
    Suspend = 2,
    Cancel = 3,
    EnableAutoRenew = 4,
    DisableAutoRenew = 5,
    ChangePlan = 6,
    Renew = 7,
    ScheduleCancellation = 8
}

public sealed record ManageSubscriptionLifecycleCommand(
    Guid OrganizationId,
    Guid SubscriptionId,
    SubscriptionLifecycleAction Action,
    Guid? NewPlanId = null,
    string? Reason = null,
    DateTime? RenewalEndDate = null) : IRequest<Result>;

public sealed class ManageSubscriptionLifecycleCommandValidator : AbstractValidator<ManageSubscriptionLifecycleCommand>
{
    public ManageSubscriptionLifecycleCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();

        RuleFor(x => x.Action)
            .Must(action => action is
                SubscriptionLifecycleAction.Activate or
                SubscriptionLifecycleAction.Suspend or
                SubscriptionLifecycleAction.Cancel or
                SubscriptionLifecycleAction.EnableAutoRenew or
                SubscriptionLifecycleAction.DisableAutoRenew or
                SubscriptionLifecycleAction.ChangePlan or
                SubscriptionLifecycleAction.Renew or
                SubscriptionLifecycleAction.ScheduleCancellation)
            .WithMessage("Unsupported subscription lifecycle action.");

        RuleFor(x => x.NewPlanId)
            .NotEmpty()
            .When(x => x.Action == SubscriptionLifecycleAction.ChangePlan)
            .WithMessage("NewPlanId is required when changing subscription plan.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Action == SubscriptionLifecycleAction.ScheduleCancellation)
            .WithMessage("Reason is required when scheduling subscription cancellation.");

        RuleFor(x => x.RenewalEndDate)
            .NotNull()
            .When(x => x.Action == SubscriptionLifecycleAction.Renew)
            .WithMessage("RenewalEndDate is required when renewing a subscription.");

        RuleFor(x => x.RenewalEndDate)
            .Must(d => d.HasValue && d.Value > DateTime.UtcNow)
            .When(x => x.Action == SubscriptionLifecycleAction.Renew)
            .WithMessage("RenewalEndDate must be in the future.");

    }
}
