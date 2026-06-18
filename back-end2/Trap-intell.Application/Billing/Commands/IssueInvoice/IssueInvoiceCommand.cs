using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.IssueInvoice;

public sealed record IssueInvoiceCommand(
    Guid OrganizationId,
    Guid InvoiceId,
    int DaysDue = 30) : IRequest<Result>;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.DaysDue).InclusiveBetween(1, 365);
    }
}
