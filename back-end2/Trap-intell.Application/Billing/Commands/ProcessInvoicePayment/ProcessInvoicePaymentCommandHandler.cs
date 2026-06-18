using MediatR;
using Trap_Intel.Application.Billing.Services;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;

internal sealed class ProcessInvoicePaymentCommandHandler : IRequestHandler<ProcessInvoicePaymentCommand, Result<Guid>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IPostPaymentSubscriptionRenewalService _postPaymentSubscriptionRenewalService;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessInvoicePaymentCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IPaymentProcessor paymentProcessor,
        IPostPaymentSubscriptionRenewalService postPaymentSubscriptionRenewalService,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _paymentProcessor = paymentProcessor;
        _postPaymentSubscriptionRenewalService = postPaymentSubscriptionRenewalService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ProcessInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<Guid>(BillingErrors.InvoiceNotFound);
        }

        var idempotencyKey = BillingIdempotency.NormalizeKey(request.IdempotencyKey);
        if (idempotencyKey is not null &&
            invoice.Status == InvoiceStatus.Paid &&
            invoice.PaymentId.HasValue)
        {
            return Result.Success(invoice.PaymentId.Value);
        }

        var paymentService = new ProcessPaymentService(
            _invoiceRepository,
            _paymentMethodRepository,
            _paymentProcessor);

        Result<Guid> paymentResult;
        if (request.PaymentMethodId.HasValue)
        {
            paymentResult = idempotencyKey is null
                ? await paymentService.ProcessAsync(request.InvoiceId, request.PaymentMethodId.Value, cancellationToken: cancellationToken)
                : await paymentService.ProcessAsync(request.InvoiceId, request.PaymentMethodId.Value, idempotencyKey, cancellationToken);
        }
        else
        {
            paymentResult = idempotencyKey is null
                ? await paymentService.ProcessWithDefaultAsync(request.InvoiceId, request.OrganizationId, cancellationToken: cancellationToken)
                : await paymentService.ProcessWithDefaultAsync(request.InvoiceId, request.OrganizationId, idempotencyKey, cancellationToken);
        }

        if (paymentResult.IsFailure)
        {
            return Result.Failure<Guid>(paymentResult.Errors);
        }

        await _postPaymentSubscriptionRenewalService.TryRenewAfterPaidInvoiceAsync(invoice, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(paymentResult.Value);
    }
}
