using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Billing.Commands.CreatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.DeactivatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.SetDefaultPaymentMethod;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.PaymentMethods;

public class PaymentMethodEndpointsNegativePathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PaymentMethodEndpointsNegativePathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePaymentMethod_WhenDefaultConflictOccurs_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CreatePaymentMethodCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(BillingErrors.PaymentMethodDefaultConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/payment-methods")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            type = "CreditCard",
            lastFourDigits = "4242",
            cardBrand = "Visa",
            paymentProcessor = "Stripe",
            token = "pm_test_api_conflict_001",
            expiresAt = DateTime.UtcNow.AddYears(1),
            billingContactEmail = "billing@example.com",
            isDefault = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SetDefaultPaymentMethod_WhenDefaultConflictOccurs_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<SetDefaultPaymentMethodCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(BillingErrors.PaymentMethodDefaultConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/payment-methods/{paymentMethodId}/set-default")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatePaymentMethod_WhenDefaultConflictOccurs_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<DeactivatePaymentMethodCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(BillingErrors.PaymentMethodDefaultConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/payment-methods/{paymentMethodId}/deactivate")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            reason = "Card compromise"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
