using Moq;
using Trap_Intel.Application.Billing.Commands.CreatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.DeactivatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.SetDefaultPaymentMethod;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Tests.Billing;

public class PaymentMethodDefaultConflictCommandHandlerTests
{
    [Fact]
    public async Task CreatePaymentMethod_WhenSaveChangesDetectsDefaultConflict_ReturnsConflictError()
    {
        var organizationId = Guid.NewGuid();

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.CountByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        paymentMethodRepository
            .Setup(repository => repository.GetDefaultByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentMethod?)null);
        paymentMethodRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Concurrent default payment method update."));

        var handler = new CreatePaymentMethodCommandHandler(paymentMethodRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new CreatePaymentMethodCommand(
                OrganizationId: organizationId,
                Type: PaymentMethodType.CreditCard,
                LastFourDigits: "4242",
                CardBrand: "Visa",
                PaymentProcessor: "Stripe",
                Token: "pm_test_conflict_001",
                ExpiresAt: DateTime.UtcNow.AddYears(1),
                BillingContactEmail: "billing@example.com",
                IsDefault: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentMethod.DefaultConflict", result.Errors[0].Code);
    }

    [Fact]
    public async Task SetDefaultPaymentMethod_WhenSaveChangesDetectsDefaultConflict_ReturnsConflictError()
    {
        var organizationId = Guid.NewGuid();
        var targetMethod = CreateActivePaymentMethod(organizationId);
        var existingDefault = CreateActivePaymentMethod(organizationId);
        existingDefault.SetAsDefault();

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(targetMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetMethod);
        paymentMethodRepository
            .Setup(repository => repository.GetDefaultByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDefault);
        paymentMethodRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Concurrent default payment method update."));

        var handler = new SetDefaultPaymentMethodCommandHandler(paymentMethodRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new SetDefaultPaymentMethodCommand(organizationId, targetMethod.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentMethod.DefaultConflict", result.Errors[0].Code);
    }

    [Fact]
    public async Task DeactivatePaymentMethod_WhenSaveChangesDetectsDefaultConflict_ReturnsConflictError()
    {
        var organizationId = Guid.NewGuid();
        var defaultMethod = CreateActivePaymentMethod(organizationId);
        defaultMethod.SetAsDefault();
        var fallbackMethod = CreateActivePaymentMethod(organizationId);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(defaultMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultMethod);
        paymentMethodRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        paymentMethodRepository
            .Setup(repository => repository.GetActiveByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fallbackMethod });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Concurrent default payment method update."));

        var handler = new DeactivatePaymentMethodCommandHandler(paymentMethodRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new DeactivatePaymentMethodCommand(organizationId, defaultMethod.Id, "Card compromised"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentMethod.DefaultConflict", result.Errors[0].Code);
    }

    private static PaymentMethod CreateActivePaymentMethod(Guid organizationId)
    {
        var details = new PaymentMethodDetails(
            lastFourDigits: "4242",
            cardBrand: "Visa",
            paymentProcessor: "Stripe",
            token: "pm_test_default_conflict",
            expiresAt: DateTime.UtcNow.AddYears(1),
            billingContactEmail: "billing@example.com");

        var createResult = PaymentMethod.Create(organizationId, PaymentMethodType.CreditCard, details);
        Assert.True(createResult.IsSuccess);

        return createResult.Value;
    }
}
