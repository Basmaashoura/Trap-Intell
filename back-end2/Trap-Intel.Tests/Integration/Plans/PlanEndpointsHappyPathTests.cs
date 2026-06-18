using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Plans.Queries.GetPlanById;
using Trap_Intel.Application.Plans.Queries.GetPlans;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Plans.ValueObjects;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Plans;

public class PlanEndpointsHappyPathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PlanEndpointsHappyPathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllPlans_WithManagePlansAuthorization_ReturnsOk_AndIncludesInactiveQuery()
    {
        IReadOnlyList<PlanSummaryDto> expected =
        [
            new PlanSummaryDto(
                Guid.NewGuid(),
                "Growth",
                "Growth plan",
                PlanType.Paid,
                false,
                149m,
                "USD")
        ];

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetPlansQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/plans/all?type=Paid")
            .WithTestAuth(Guid.NewGuid(), Permissions.System.ManagePlans);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<PlanSummaryDto>>();
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal(expected[0].Id, body[0].Id);

        sender.Verify(
            x => x.Send(
                It.Is<GetPlansQuery>(q => q.Type == PlanType.Paid && q.IncludeInactive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPlanPricing_WhenPlanExists_ReturnsPricingProjection()
    {
        var planId = Guid.NewGuid();

        IReadOnlyList<PlanPricingDto> pricing =
        [
            new PlanPricingDto(BillingCycle.Monthly, 199m, "USD", 0m)
        ];

        IReadOnlyList<PlanFeatureDto> features =
        [
            new PlanFeatureDto(
                "alerts",
                "Alerts",
                "Alerting capabilities",
                FeatureCategory.Alerting,
                true,
                null,
                null,
                false,
                1)
        ];

        var detail = new PlanDetailDto(
            planId,
            "Growth",
            "Growth plan",
            PlanType.Paid,
            CustomizationLevel.Basic,
            true,
            new PlanSupportTierDto(SupportLevel.Priority, 30, false),
            new PlanComplianceDto(ComplianceLevel.SOC2, ["SOC2"], true),
            pricing,
            features,
            null,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetPlanByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(detail));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{planId}/pricing")
            .WithTestAuth(Guid.NewGuid(), Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);

        Assert.Equal(planId, json.RootElement.GetProperty("planId").GetGuid());

        var pricingArray = json.RootElement.GetProperty("pricing");
        Assert.Equal(1, pricingArray.GetArrayLength());
        Assert.Equal("Monthly", pricingArray[0].GetProperty("billingCycle").GetString());
        Assert.Equal(199m, pricingArray[0].GetProperty("amount").GetDecimal());

        sender.Verify(
            x => x.Send(
                It.Is<GetPlanByIdQuery>(q => q.PlanId == planId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPlanQuotaTemplate_WhenQuotaMissing_ReturnsHasQuotaTemplateFalse()
    {
        var planId = Guid.NewGuid();

        var detail = new PlanDetailDto(
            planId,
            "Starter",
            "Starter plan",
            PlanType.Free,
            CustomizationLevel.None,
            true,
            new PlanSupportTierDto(SupportLevel.Basic, 120, false),
            new PlanComplianceDto(ComplianceLevel.None, [], false),
            [new PlanPricingDto(BillingCycle.Monthly, 0m, "USD", 0m)],
            [],
            null,
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow.AddDays(-1));

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetPlanByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(detail));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{planId}/quota-template")
            .WithTestAuth(Guid.NewGuid(), Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);

        Assert.Equal(planId, json.RootElement.GetProperty("planId").GetGuid());
        Assert.False(json.RootElement.GetProperty("hasQuotaTemplate").GetBoolean());
    }

    [Fact]
    public async Task GetPlans_WithInvalidType_ReturnsBadRequest_AndDoesNotCallSender()
    {
        var sender = new Mock<ISender>();
        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/plans/?type=NotAPlanType")
            .WithTestAuth(Guid.NewGuid(), Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        sender.Verify(
            x => x.Send(It.IsAny<GetPlansQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
