using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.SetDefaultPaymentMethod;

public sealed record SetDefaultPaymentMethodCommand(
    Guid OrganizationId,
    Guid PaymentMethodId) : IRequest<Result>;

public sealed class SetDefaultPaymentMethodCommandValidator : AbstractValidator<SetDefaultPaymentMethodCommand>
{
    public SetDefaultPaymentMethodCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}
