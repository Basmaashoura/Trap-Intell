using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.DeactivatePaymentMethod;

internal sealed class DeactivatePaymentMethodCommandHandler : IRequestHandler<DeactivatePaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository, IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || paymentMethod.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(BillingErrors.PaymentMethodNotFound);
        }

        var wasDefault = paymentMethod.IsDefault;

        var deactivateResult = paymentMethod.Deactivate(request.Reason.Trim());
        if (deactivateResult.IsFailure)
        {
            return Result.Failure(deactivateResult.Errors);
        }

        if (wasDefault)
        {
            paymentMethod.UnsetAsDefault();
        }

        await _paymentMethodRepository.UpdateAsync(paymentMethod, cancellationToken);

        if (wasDefault)
        {
            var activeMethods = await _paymentMethodRepository.GetActiveByOrganizationIdAsync(request.OrganizationId, cancellationToken);
            var candidate = activeMethods
                .Where(method => method.Id != paymentMethod.Id)
                .OrderByDescending(method => method.UpdatedAt)
                .FirstOrDefault();

            if (candidate is not null)
            {
                candidate.SetAsDefault();
                await _paymentMethodRepository.UpdateAsync(candidate, cancellationToken);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(BillingErrors.PaymentMethodDefaultConflict);
        }

        return Result.Success();
    }
}
