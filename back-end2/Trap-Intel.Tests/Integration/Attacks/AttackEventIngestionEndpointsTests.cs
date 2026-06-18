using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Attacks.Commands.IngestAttackEvent;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Attacks;

public class AttackEventIngestionEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AttackEventIngestionEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IngestAttackEvent_WhenCommandSucceeds_ReturnsCreated()
    {
        var organizationId = Guid.NewGuid();
        var honeypotId = Guid.NewGuid();
        var attackEventId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<IngestAttackEventCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(attackEventId));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks/events")
            .WithTestAuth(organizationId);

        request.Content = JsonContent.Create(CreatePayload("evt-100"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith(
            $"/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks/events/{attackEventId}",
            response.Headers.Location!.OriginalString,
            StringComparison.OrdinalIgnoreCase);

        sender.Verify(
            x => x.Send(
                It.Is<IngestAttackEventCommand>(command =>
                    command.OrganizationId == organizationId &&
                    command.HoneypotId == honeypotId &&
                    command.ExternalEventId == "evt-100"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IngestAttackEvent_WhenHoneypotMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var honeypotId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<IngestAttackEventCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(Error.Custom("Honeypot.NotFound", "Honeypot not found.")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks/events")
            .WithTestAuth(organizationId);

        request.Content = JsonContent.Create(CreatePayload("evt-404"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IngestAttackEvent_WhenValidationOrDomainFails_ReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        var honeypotId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<IngestAttackEventCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(Error.Custom("Attack.Ingest.Invalid", "Invalid attack event payload.")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks/events")
            .WithTestAuth(organizationId);

        request.Content = JsonContent.Create(CreatePayload("evt-400"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestAttackEvent_WhenOrganizationClaimMismatchesRoute_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();
        var honeypotId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{routeOrganizationId}/honeypots/{honeypotId}/attacks/events")
            .WithTestAuth(claimOrganizationId);

        request.Content = JsonContent.Create(CreatePayload("evt-403"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        sender.Verify(
            x => x.Send(It.IsAny<IngestAttackEventCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static object CreatePayload(string externalEventId)
    {
        return new
        {
            externalEventId,
            timestamp = DateTime.UtcNow,
            eventType = "ssh_brute_force",
            severity = "high",
            sourceIP = "198.51.100.24",
            sourcePort = 55444,
            targetPort = 22,
            sensorId = "sensor-1",
            protocol = "ssh",
            sessionId = 12345,
            wasEdgeFiltered = false,
            filterReason = (string?)null,
            username = "root",
            password = "toor",
            command = "uname -a",
            payload = (byte[]?)null,
            userAgent = "attack-bot",
            headers = new Dictionary<string, string> { ["x-request-id"] = "req-1" },
            geolocation = new
            {
                country = "Unknown",
                countryCode = "XX",
                city = "Unknown",
                latitude = (decimal?)null,
                longitude = (decimal?)null,
                region = (string?)null,
                isp = (string?)null,
                asn = (string?)null
            },
            rawPayloadJson = "{\"sample\":true}"
        };
    }
}
