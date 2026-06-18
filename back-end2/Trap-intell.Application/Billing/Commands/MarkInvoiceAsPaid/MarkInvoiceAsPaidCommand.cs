using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.MarkInvoiceAsPaid;

public sealed record MarkInvoiceAsPaidCommand(
    Guid OrganizationId,
    Guid InvoiceId,
    Guid PaymentId) : IRequest<Result>;

public sealed class MarkInvoiceAsPaidCommandValidator : AbstractValidator<MarkInvoiceAsPaidCommand>
{
    public MarkInvoiceAsPaidCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}
