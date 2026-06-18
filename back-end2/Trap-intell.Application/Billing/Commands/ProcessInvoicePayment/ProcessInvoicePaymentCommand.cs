using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;

public sealed record ProcessInvoicePaymentCommand(
    Guid OrganizationId,
    Guid InvoiceId,
    Guid? PaymentMethodId = null,
    string? IdempotencyKey = null) : IRequest<Result<Guid>>;

public sealed class ProcessInvoicePaymentCommandValidator : AbstractValidator<ProcessInvoicePaymentCommand>
{
    public ProcessInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(BillingIdempotency.MaxKeyLength)
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));
    }
}
