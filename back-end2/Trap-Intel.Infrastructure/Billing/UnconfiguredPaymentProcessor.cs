using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Infrastructure.Billing;

internal sealed class UnconfiguredPaymentProcessor : IPaymentProcessor
{
    private static readonly Error NotConfiguredError = Error.Custom(
        "Billing.PaymentProcessorNotConfigured",
        "Payment processor is not configured for this environment.");

    public Task<Result<Guid>> ChargeAsync(
        PaymentMethod paymentMethod,
        decimal amount,
        string currency,
        string invoiceNumber,
        string description,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Guid>(NotConfiguredError));
    }

    public Task<Result<Guid>> ChargeAsync(
        PaymentMethod paymentMethod,
        decimal amount,
        string currency,
        string invoiceNumber,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Guid>(NotConfiguredError));
    }

    public Task<Result<Guid>> RefundAsync(
        Guid paymentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Guid>(NotConfiguredError));
    }

    public Task<Result<Guid>> RefundAsync(
        Guid paymentId,
        decimal amount,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Guid>(NotConfiguredError));
    }

    public Task<Result<bool>> VerifyAsync(
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<bool>(NotConfiguredError));
    }
}
