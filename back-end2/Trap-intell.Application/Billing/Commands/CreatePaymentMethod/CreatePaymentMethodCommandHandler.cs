using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Application.Billing.Security;

namespace Trap_Intel.Application.Billing.Commands.CreatePaymentMethod;

internal sealed class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository, IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        PaymentMethodDetails details;
        try
        {
            details = new PaymentMethodDetails(
                request.LastFourDigits,
                request.CardBrand,
                request.PaymentProcessor,
                PaymentTokenProtector.Protect(request.Token),
                request.ExpiresAt,
                request.BillingContactEmail);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<Guid>(Error.Custom("PaymentMethod.InvalidDetails", exception.Message));
        }

        var createResult = PaymentMethod.Create(request.OrganizationId, request.Type, details);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Errors);
        }

        var paymentMethod = createResult.Value;

        var paymentMethodsCount = await _paymentMethodRepository.CountByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        var shouldSetDefault = request.IsDefault || paymentMethodsCount == 0;

        if (shouldSetDefault)
        {
            var existingDefault = await _paymentMethodRepository.GetDefaultByOrganizationIdAsync(request.OrganizationId, cancellationToken);
            if (existingDefault is not null)
            {
                existingDefault.UnsetAsDefault();
                await _paymentMethodRepository.UpdateAsync(existingDefault, cancellationToken);
            }

            paymentMethod.SetAsDefault();
        }

        await _paymentMethodRepository.AddAsync(paymentMethod, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<Guid>(BillingErrors.PaymentMethodDefaultConflict);
        }

        return Result.Success(paymentMethod.Id);
    }
}
