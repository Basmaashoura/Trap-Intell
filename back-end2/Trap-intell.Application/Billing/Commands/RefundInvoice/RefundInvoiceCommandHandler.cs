using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.RefundInvoice;

internal sealed class RefundInvoiceCommandHandler : IRequestHandler<RefundInvoiceCommand, Result<Guid>>
{
    private static readonly Error MissingPaymentReferenceError = Error.Custom(
        "Invoice.PaymentReferenceMissing",
        "Invoice payment reference is required before a refund can be processed.");

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IUnitOfWork _unitOfWork;

    public RefundInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPaymentProcessor paymentProcessor,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _paymentProcessor = paymentProcessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RefundInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<Guid>(BillingErrors.InvoiceNotFound);
        }

        if (!invoice.PaymentId.HasValue)
        {
            return Result.Failure<Guid>(MissingPaymentReferenceError);
        }

        var idempotencyKey = BillingIdempotency.NormalizeKey(request.IdempotencyKey);
        if (invoice.Status == InvoiceStatus.Refunded && idempotencyKey is not null)
        {
            return Result.Success(BillingIdempotency.CreateRefundOperationId(invoice.PaymentId.Value, idempotencyKey));
        }

        if (invoice.Status != InvoiceStatus.Paid)
        {
            return Result.Failure<Guid>(BillingErrors.InvoiceCannotRefund);
        }

        if (request.RefundAmount > invoice.Amount.TotalAmount)
        {
            return Result.Failure<Guid>(BillingErrors.InvoiceRefundExceedsAmount);
        }

        var refundResult = idempotencyKey is null
            ? await _paymentProcessor.RefundAsync(
                invoice.PaymentId.Value,
                request.RefundAmount,
                request.Reason,
                cancellationToken)
            : await _paymentProcessor.RefundAsync(
                invoice.PaymentId.Value,
                request.RefundAmount,
                request.Reason,
                idempotencyKey,
                cancellationToken);

        if (refundResult.IsFailure)
        {
            return Result.Failure<Guid>(refundResult.Errors);
        }

        var invoiceRefundResult = invoice.Refund(request.RefundAmount, request.Reason);
        if (invoiceRefundResult.IsFailure)
        {
            return Result.Failure<Guid>(invoiceRefundResult.Errors);
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(refundResult.Value);
    }
}
