using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Commands.SetSubscriptionPaymentMethod;

internal sealed class SetSubscriptionPaymentMethodCommandHandler : IRequestHandler<SetSubscriptionPaymentMethodCommand, Result>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetSubscriptionPaymentMethodCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetSubscriptionPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(SubscriptionErrors.SubscriptionNotFound);
        }

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

        subscription.SetPaymentMethod(paymentMethod.Id);
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(SubscriptionErrors.ConcurrencyConflict);
        }

        return Result.Success();
    }
}
