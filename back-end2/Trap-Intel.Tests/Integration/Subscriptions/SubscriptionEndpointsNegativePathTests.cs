using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionUsage;
using Trap_Intel.Application.Subscriptions.Commands.SetSubscriptionPaymentMethod;
using Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Subscriptions;

public class SubscriptionEndpointsNegativePathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SubscriptionEndpointsNegativePathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RenewSubscription_WhenStatusTransitionConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Custom("Subscription.InvalidStatusTransition", "Invalid transition.")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/renew")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            renewalEndDate = DateTime.UtcNow.AddMonths(2)
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentQuota_WhenSubscriptionMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetCurrentOrganizationSubscriptionQuotaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SubscriptionQuotaUsageDto>(SubscriptionErrors.SubscriptionNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/current/quota")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleCancellation_WhenSubscriptionMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.SubscriptionNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/schedule-cancel")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            reason = "Not needed anymore"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePlan_WhenPricingMissing_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(PlanErrors.PricingNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/change-plan")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            planId = Guid.NewGuid()
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordUsageSnapshot_WithInvalidPeriodType_ReturnsBadRequest_AndDoesNotCallSender()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/snapshots")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            honeypotsActive = 2,
            storageUsedGb = 1.5m,
            apiCallsCount = 100,
            activeUsers = 3,
            eventsCaptured = 20,
            periodType = "UnknownPeriod"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        sender.Verify(
            x => x.Send(It.IsAny<ManageSubscriptionUsageCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordUsageSnapshot_WhenHardLimitEnforced_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(QuotaErrors.HardLimitEnforced));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/snapshots")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            honeypotsActive = 2,
            storageUsedGb = 1.5m,
            apiCallsCount = 100,
            activeUsers = 3,
            eventsCaptured = 20,
            periodType = "Daily"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CheckQuotaOperation_WhenQuotaMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CheckSubscriptionQuotaOperationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SubscriptionQuotaOperationCheckDto>(QuotaErrors.QuotaNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/quota/check?additionalHoneypots=1&additionalStorageGb=0.5")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetPaymentMethod_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<SetSubscriptionPaymentMethodCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/payment-method")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            paymentMethodId = Guid.NewGuid()
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubscription_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            planId = Guid.NewGuid(),
            billingCycle = "Monthly",
            isTrial = false,
            trialDays = 14,
            activateImmediately = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ActivateSubscription_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/activate")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordUsageSnapshot_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/snapshots")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            honeypotsActive = 2,
            storageUsedGb = 1.5m,
            apiCallsCount = 100,
            activeUsers = 3,
            eventsCaptured = 20,
            periodType = "Daily"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FinalizeMonthlyUsage_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        const int year = 2026;
        const int month = 4;

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/monthly/{year}/{month}/finalize")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task MarkMonthlyUsageAsBilled_WhenConcurrencyConflict_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        const int year = 2026;
        const int month = 4;

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(SubscriptionErrors.ConcurrencyConflict));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/monthly/{year}/{month}/mark-billed")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            invoiceId = Guid.NewGuid()
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
