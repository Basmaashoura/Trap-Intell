using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Commands.SetSubscriptionPaymentMethod;

public sealed record SetSubscriptionPaymentMethodCommand(
    Guid OrganizationId,
    Guid SubscriptionId,
    Guid PaymentMethodId) : IRequest<Result>;

public sealed class SetSubscriptionPaymentMethodCommandValidator : AbstractValidator<SetSubscriptionPaymentMethodCommand>
{
    public SetSubscriptionPaymentMethodCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}
