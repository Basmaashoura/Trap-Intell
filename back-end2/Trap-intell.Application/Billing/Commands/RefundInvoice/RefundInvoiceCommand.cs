using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.RefundInvoice;

public sealed record RefundInvoiceCommand(
    Guid OrganizationId,
    Guid InvoiceId,
    decimal RefundAmount,
    string Reason,
    string? IdempotencyKey = null) : IRequest<Result<Guid>>;

public sealed class RefundInvoiceCommandValidator : AbstractValidator<RefundInvoiceCommand>
{
    public RefundInvoiceCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.RefundAmount).GreaterThan(0);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(BillingIdempotency.MaxKeyLength)
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));
    }
}
