using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Infrastructure.Billing;
using Trap_Intel.Infrastructure.Configuration;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class StripePaymentProcessorTests
{
    [Fact]
    public async Task ChargeAsync_WithIdempotencyKey_SendsHeaderAndReturnsDeterministicPaymentId()
    {
        var organizationId = Guid.NewGuid();
        const string invoiceNumber = "INV-STRIPE-CHARGE-001";
        const string idempotencyKey = "charge-op-001";

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            invoiceNumber,
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-2),
            dueDate: DateTime.UtcNow.AddDays(7));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(repository => repository.GetByInvoiceNumberAsync(invoiceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(repository => repository.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentMethod = CreatePaymentMethod(organizationId);

        var handler = new CaptureHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/payment_intents", request.RequestUri?.ToString(), StringComparison.Ordinal);

            Assert.True(request.Headers.TryGetValues("Idempotency-Key", out var values));
            Assert.Equal(idempotencyKey, values.Single());

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"pi_test_123\",\"status\":\"succeeded\"}", Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.stripe.com/v1/")
        };

        var settings = Options.Create(new PaymentGatewaySettings
        {
            Provider = "Stripe",
            StripeSecretKey = "sk_test_123",
            StripeApiBaseUrl = "https://api.stripe.com/v1/"
        });

        var processor = new StripePaymentProcessor(
            httpClient,
            settings,
            NullLogger<StripePaymentProcessor>.Instance,
            invoiceRepository.Object);

        var result = await processor.ChargeAsync(
            paymentMethod,
            amount: 25m,
            currency: "USD",
            invoiceNumber: invoiceNumber,
            description: "Test charge",
            idempotencyKey: idempotencyKey,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BillingIdempotency.CreatePaymentOperationId(invoiceNumber, idempotencyKey), result.Value);

        invoiceRepository.Verify(
            repository => repository.GetByInvoiceNumberAsync(invoiceNumber, It.IsAny<CancellationToken>()),
            Times.Once);
        invoiceRepository.Verify(
            repository => repository.UpdateAsync(invoice, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefundAsync_WhenInMemoryReferenceIsMissing_RestoresReferenceFromInvoiceNotes()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const string providerReference = "pi_test_restart_fallback";

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-STRIPE-REFUND-001",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-10),
            dueDate: DateTime.UtcNow.AddDays(-5),
            paymentId: paymentId);

        var noteResult = invoice.AddNote($"PaymentProviderReference:stripe:{paymentId:N}:{providerReference}");
        Assert.True(noteResult.IsSuccess);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(repository => repository.GetByPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var handler = new CaptureHttpMessageHandler((request, _) =>
        {
            var payload = request.Content is null
                ? string.Empty
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("payment_intent=pi_test_restart_fallback", payload, StringComparison.Ordinal);
            Assert.Contains("amount=2500", payload, StringComparison.Ordinal);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"re_test_123\",\"status\":\"succeeded\"}", Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.stripe.com/v1/")
        };

        var settings = Options.Create(new PaymentGatewaySettings
        {
            Provider = "Stripe",
            StripeSecretKey = "sk_test_123",
            StripeApiBaseUrl = "https://api.stripe.com/v1/"
        });

        var processor = new StripePaymentProcessor(
            httpClient,
            settings,
            NullLogger<StripePaymentProcessor>.Instance,
            invoiceRepository.Object);

        var result = await processor.RefundAsync(
            paymentId,
            amount: 25m,
            reason: "Customer request",
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);

        invoiceRepository.Verify(
            repository => repository.GetByPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class CaptureHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public CaptureHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    private static PaymentMethod CreatePaymentMethod(Guid organizationId)
    {
        var details = new PaymentMethodDetails(
            lastFourDigits: "4242",
            cardBrand: "Visa",
            paymentProcessor: "Stripe",
            token: "pm_test_idempotent",
            expiresAt: DateTime.UtcNow.AddYears(2),
            billingContactEmail: "billing@trapintel.local");

        var createResult = PaymentMethod.Create(
            organizationId,
            PaymentMethodType.CreditCard,
            details);

        Assert.True(createResult.IsSuccess);
        return createResult.Value;
    }
}
