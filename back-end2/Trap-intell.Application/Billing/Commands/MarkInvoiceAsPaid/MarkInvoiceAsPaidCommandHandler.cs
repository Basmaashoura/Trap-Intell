using MediatR;
using Trap_Intel.Application.Billing.Services;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.MarkInvoiceAsPaid;

internal sealed class MarkInvoiceAsPaidCommandHandler : IRequestHandler<MarkInvoiceAsPaidCommand, Result>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPostPaymentSubscriptionRenewalService _postPaymentSubscriptionRenewalService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkInvoiceAsPaidCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPostPaymentSubscriptionRenewalService postPaymentSubscriptionRenewalService,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _postPaymentSubscriptionRenewalService = postPaymentSubscriptionRenewalService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkInvoiceAsPaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(BillingErrors.InvoiceNotFound);
        }

        var markPaidResult = invoice.MarkAsPaid(request.PaymentId);
        if (markPaidResult.IsFailure)
        {
            return Result.Failure(markPaidResult.Errors);
        }

        await _postPaymentSubscriptionRenewalService.TryRenewAfterPaidInvoiceAsync(invoice, cancellationToken);

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
