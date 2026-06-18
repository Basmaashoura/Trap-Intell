using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.DeactivatePaymentMethod;

public sealed record DeactivatePaymentMethodCommand(
    Guid OrganizationId,
    Guid PaymentMethodId,
    string Reason) : IRequest<Result>;

public sealed class DeactivatePaymentMethodCommandValidator : AbstractValidator<DeactivatePaymentMethodCommand>
{
    public DeactivatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
