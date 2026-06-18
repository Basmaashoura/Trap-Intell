using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Organizations.Queries.GetOrganizationOwnerDashboard;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Organizations;

public class OrganizationOwnerDashboardEndpointsHappyPathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OrganizationOwnerDashboardEndpointsHappyPathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOwnerDashboard_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();

        var expected = new OrganizationOwnerDashboardDto(
            organizationId,
            "Acme Org",
            "Active",
            true,
            new OrganizationOwnerSubscriptionSummaryDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Growth",
                "Paid",
                "Active",
                "Monthly",
                399m,
                0m,
                DateTime.UtcNow.AddMonths(-1),
                DateTime.UtcNow.AddMonths(11),
                DateTime.UtcNow.AddMonths(11),
                true,
                DateTime.UtcNow),
            new OrganizationOwnerQuotaSummaryDto(
                3,
                10,
                30m,
                5m,
                20m,
                25m,
                15000,
                100000,
                15m,
                false,
                false,
                0m,
                false,
                true,
                false),
            new OrganizationOwnerAlertSummaryDto(
                12,
                4,
                2,
                1,
                0,
                [new OrganizationOwnerTrendItemDto("Malware", 5)],
                [new OrganizationOwnerTrendItemDto("Critical", 2)]),
            new OrganizationOwnerAuditSummaryDto(
                120,
                1,
                8,
                [new OrganizationOwnerAuditResourceItemDto("Users", 25)],
                [new OrganizationOwnerRecentAuditEventDto(Guid.NewGuid(), "Update", "Users", DateTime.UtcNow, null)]),
            DateTime.UtcNow);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetOrganizationOwnerDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/dashboard/owner?lastNDays=30")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OrganizationOwnerDashboardDto>();
        Assert.NotNull(body);
        Assert.Equal(expected.OrganizationId, body.OrganizationId);
        Assert.Equal(expected.OrganizationName, body.OrganizationName);
        Assert.True(body.HasSubscription);

        sender.Verify(
            x => x.Send(
                It.Is<GetOrganizationOwnerDashboardQuery>(q =>
                    q.OrganizationId == organizationId &&
                    q.LastNDays == 30),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOwnerDashboard_WhenOrganizationMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetOrganizationOwnerDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OrganizationOwnerDashboardDto>(OrganizationErrors.OrganizationNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/dashboard/owner")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
