using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Application.Billing.Security;

namespace Trap_Intel.Application.Billing.Commands.UpdatePaymentMethod;

internal sealed class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository, IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || paymentMethod.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(BillingErrors.PaymentMethodNotFound);
        }

        var existingDetails = paymentMethod.Details;
        var tokenForStorage = request.Token is null
            ? PaymentTokenProtector.Protect(existingDetails.Token)
            : PaymentTokenProtector.Protect(request.Token);

        PaymentMethodDetails updatedDetails;
        try
        {
            updatedDetails = new PaymentMethodDetails(
                request.LastFourDigits ?? existingDetails.LastFourDigits,
                request.CardBrand ?? existingDetails.CardBrand,
                request.PaymentProcessor ?? existingDetails.PaymentProcessor,
                tokenForStorage,
                request.ExpiresAt ?? existingDetails.ExpiresAt,
                request.BillingContactEmail ?? existingDetails.BillingContactEmail);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure(Error.Custom("PaymentMethod.InvalidDetails", exception.Message));
        }

        var updateResult = paymentMethod.UpdateDetails(updatedDetails);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Errors);
        }

        await _paymentMethodRepository.UpdateAsync(paymentMethod, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
