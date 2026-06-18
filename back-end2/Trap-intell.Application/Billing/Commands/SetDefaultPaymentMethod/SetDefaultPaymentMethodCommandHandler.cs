using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.SetDefaultPaymentMethod;

internal sealed class SetDefaultPaymentMethodCommandHandler : IRequestHandler<SetDefaultPaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetDefaultPaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository, IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || paymentMethod.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(BillingErrors.PaymentMethodNotFound);
        }

        var usableResult = paymentMethod.ValidateForUse();
        if (usableResult.IsFailure)
        {
            return Result.Failure(usableResult.Errors);
        }

        var existingDefault = await _paymentMethodRepository.GetDefaultByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        if (existingDefault is not null && existingDefault.Id != paymentMethod.Id)
        {
            existingDefault.UnsetAsDefault();
            await _paymentMethodRepository.UpdateAsync(existingDefault, cancellationToken);
        }

        paymentMethod.SetAsDefault();
        await _paymentMethodRepository.UpdateAsync(paymentMethod, cancellationToken);

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
