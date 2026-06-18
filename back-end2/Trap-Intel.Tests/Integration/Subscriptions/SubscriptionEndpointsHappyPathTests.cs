using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using MediatR;
using Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;
using Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Subscriptions;

public class SubscriptionEndpointsHappyPathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SubscriptionEndpointsHappyPathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentSubscription_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var expected = new SubscriptionSummaryDto(
            Guid.NewGuid(),
            organizationId,
            Guid.NewGuid(),
            SubscriptionStatus.Active,
            BillingCycle.Monthly,
            149m,
            null,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            DateTime.UtcNow.AddDays(20),
            true,
            4,
            12.5m,
            0m,
            DateTime.UtcNow);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetCurrentOrganizationSubscriptionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/current")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SubscriptionSummaryDto>();
        Assert.NotNull(body);
        Assert.Equal(expected.Id, body.Id);
        Assert.Equal(expected.OrganizationId, body.OrganizationId);

        sender.Verify(
            x => x.Send(
                It.Is<GetCurrentOrganizationSubscriptionQuery>(q => q.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentSubscription_WhenNotFoundResult_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetCurrentOrganizationSubscriptionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SubscriptionSummaryDto>(SubscriptionErrors.SubscriptionNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/current")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentQuota_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var expected = new SubscriptionQuotaUsageDto(
            CurrentHoneypots: 3,
            MaxHoneypots: 10,
            HoneypotUsagePercent: 30m,
            CurrentStorageGb: 5m,
            MaxStorageGb: 50m,
            StorageUsagePercent: 10m,
            CurrentApiCalls: 400,
            MaxApiCalls: 2000,
            ApiCallsUsagePercent: 20m,
            IsApproachingLimit: false,
            IsOverLimit: false);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetCurrentOrganizationSubscriptionQuotaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/current/quota")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SubscriptionQuotaUsageDto>();
        Assert.NotNull(body);
        Assert.Equal(expected.CurrentHoneypots, body.CurrentHoneypots);
        Assert.Equal(expected.MaxHoneypots, body.MaxHoneypots);

        sender.Verify(
            x => x.Send(
                It.Is<GetCurrentOrganizationSubscriptionQuotaQuery>(q => q.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        sender.Verify(
            x => x.Send(It.IsAny<GetCurrentOrganizationSubscriptionQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateSubscription_WithValidRequest_ReturnsCreated()
    {
        var organizationId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var createdSubscriptionId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(createdSubscriptionId));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            planId,
            billingCycle = "Monthly",
            isTrial = false,
            trialDays = 14,
            activateImmediately = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/organizations/{organizationId}/subscriptions/{createdSubscriptionId}", response.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        Assert.True(json.RootElement.TryGetProperty("subscriptionId", out var subscriptionIdElement));
        Assert.Equal(createdSubscriptionId, subscriptionIdElement.GetGuid());

        sender.Verify(
            x => x.Send(
                It.Is<CreateSubscriptionCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.PlanId == planId &&
                    c.BillingCycle == BillingCycle.Monthly),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidBillingCycle_ReturnsBadRequest_AndDoesNotCallSender()
    {
        var organizationId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            planId,
            billingCycle = "NotARealCycle",
            isTrial = false,
            trialDays = 14,
            activateImmediately = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        sender.Verify(
            x => x.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewSubscription_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var renewalEnd = DateTime.UtcNow.AddMonths(1);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ManageSubscriptionLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/renew")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            renewalEndDate = renewalEnd
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<ManageSubscriptionLifecycleCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.SubscriptionId == subscriptionId &&
                    c.Action == SubscriptionLifecycleAction.Renew),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckQuotaOperation_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var expected = new SubscriptionQuotaOperationCheckDto(
            subscriptionId,
            3,
            10,
            4.5m,
            20m,
            1,
            2m,
            4,
            6.5m,
            false,
            false,
            true,
            "Operation is allowed for requested quota usage.");

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CheckSubscriptionQuotaOperationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/quota/check?additionalHoneypots=1&additionalStorageGb=2")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SubscriptionQuotaOperationCheckDto>();
        Assert.NotNull(body);
        Assert.Equal(expected.SubscriptionId, body.SubscriptionId);
        Assert.Equal(expected.IsAllowed, body.IsAllowed);
    }
}
