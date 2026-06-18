using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionUsage;

public enum SubscriptionUsageAction
{
    RecordSnapshot = 1,
    FinalizeMonthlyUsage = 2,
    MarkMonthlyUsageAsBilled = 3
}

public sealed record ManageSubscriptionUsageCommand(
    Guid OrganizationId,
    Guid SubscriptionId,
    SubscriptionUsageAction Action,
    int? HoneypotsActive = null,
    decimal? StorageUsedGb = null,
    int ApiCallsCount = 0,
    int ActiveUsers = 0,
    int EventsCaptured = 0,
    UsagePeriodType PeriodType = UsagePeriodType.Daily,
    int? Year = null,
    int? Month = null,
    Guid? InvoiceId = null) : IRequest<Result>;

public sealed class ManageSubscriptionUsageCommandValidator : AbstractValidator<ManageSubscriptionUsageCommand>
{
    public ManageSubscriptionUsageCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();

        RuleFor(x => x.Action)
            .Must(action => action is
                SubscriptionUsageAction.RecordSnapshot or
                SubscriptionUsageAction.FinalizeMonthlyUsage or
                SubscriptionUsageAction.MarkMonthlyUsageAsBilled)
            .WithMessage("Unsupported subscription usage action.");

        RuleFor(x => x.HoneypotsActive)
            .NotNull()
            .When(x => x.Action == SubscriptionUsageAction.RecordSnapshot)
            .WithMessage("HoneypotsActive is required when recording usage snapshot.");

        RuleFor(x => x.StorageUsedGb)
            .NotNull()
            .When(x => x.Action == SubscriptionUsageAction.RecordSnapshot)
            .WithMessage("StorageUsedGb is required when recording usage snapshot.");

        RuleFor(x => x.HoneypotsActive)
            .GreaterThanOrEqualTo(0)
            .When(x => x.HoneypotsActive.HasValue);

        RuleFor(x => x.StorageUsedGb)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StorageUsedGb.HasValue);

        RuleFor(x => x.ApiCallsCount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ActiveUsers)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.EventsCaptured)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Year)
            .NotNull()
            .When(x => x.Action is SubscriptionUsageAction.FinalizeMonthlyUsage or SubscriptionUsageAction.MarkMonthlyUsageAsBilled)
            .WithMessage("Year is required for monthly usage actions.");

        RuleFor(x => x.Month)
            .NotNull()
            .When(x => x.Action is SubscriptionUsageAction.FinalizeMonthlyUsage or SubscriptionUsageAction.MarkMonthlyUsageAsBilled)
            .WithMessage("Month is required for monthly usage actions.");

        RuleFor(x => x.InvoiceId)
            .NotNull()
            .When(x => x.Action == SubscriptionUsageAction.MarkMonthlyUsageAsBilled)
            .WithMessage("InvoiceId is required when marking monthly usage as billed.");

        RuleFor(x => x.InvoiceId)
            .NotEqual(Guid.Empty)
            .When(x => x.Action == SubscriptionUsageAction.MarkMonthlyUsageAsBilled && x.InvoiceId.HasValue)
            .WithMessage("InvoiceId must not be empty.");
    }
}
