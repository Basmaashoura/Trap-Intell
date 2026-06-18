using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.IssueInvoice;

internal sealed class IssueInvoiceCommandHandler : IRequestHandler<IssueInvoiceCommand, Result>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(BillingErrors.InvoiceNotFound);
        }

        var issueResult = invoice.Issue(request.DaysDue);
        if (issueResult.IsFailure)
        {
            return Result.Failure(issueResult.Errors);
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
