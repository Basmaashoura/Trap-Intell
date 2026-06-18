using Moq;
using Trap_Intel.Application.Attacks.Commands.IngestAttackEvent;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Tests.Attacks;

public class IngestAttackEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenHoneypotNotFound_ReturnsNotFoundFailure()
    {
        var attackRepository = new Mock<IAttackEventRepository>();
        var honeypotRepository = new Mock<IHoneypotRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new IngestAttackEventCommandHandler(
            attackRepository.Object,
            honeypotRepository.Object,
            unitOfWork.Object);

        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid(), "evt-1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Honeypot.NotFound", result.Errors[0].Code);

        attackRepository.Verify(repository => repository.AddAsync(It.IsAny<AttackEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExternalEventAlreadyExists_ReturnsExistingIdWithoutPersisting()
    {
        var organizationId = Guid.NewGuid();
        var honeypot = CreateHoneypot(organizationId, Guid.NewGuid());

        var existingEvent = CreateAttackEvent(honeypot.Id, organizationId, "evt-duplicate");

        var attackRepository = new Mock<IAttackEventRepository>();
        attackRepository
            .Setup(repository => repository.GetByExternalIdAsync("evt-duplicate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new IngestAttackEventCommandHandler(
            attackRepository.Object,
            honeypotRepository.Object,
            unitOfWork.Object);

        var command = CreateCommand(organizationId, honeypot.Id, "evt-duplicate");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingEvent.Id, result.Value);

        attackRepository.Verify(repository => repository.AddAsync(It.IsAny<AttackEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_PersistsAttackEventAndReturnsId()
    {
        var organizationId = Guid.NewGuid();
        var honeypot = CreateHoneypot(organizationId, Guid.NewGuid());

        var attackRepository = new Mock<IAttackEventRepository>();
        attackRepository
            .Setup(repository => repository.GetByExternalIdAsync("evt-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttackEvent?)null);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new IngestAttackEventCommandHandler(
            attackRepository.Object,
            honeypotRepository.Object,
            unitOfWork.Object);

        var command = CreateCommand(organizationId, honeypot.Id, "evt-new");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        attackRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AttackEvent>(attackEvent =>
                    attackEvent.HoneypotId == honeypot.Id &&
                    attackEvent.OrganizationId == organizationId &&
                    attackEvent.ExternalEventId == "evt-new"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IngestAttackEventCommand CreateCommand(Guid organizationId, Guid honeypotId, string externalEventId)
    {
        return new IngestAttackEventCommand(
            OrganizationId: organizationId,
            HoneypotId: honeypotId,
            ExternalEventId: externalEventId,
            Timestamp: DateTime.UtcNow,
            EventType: "ssh_brute_force",
            Severity: "high",
            SourceIP: "185.220.101.42",
            SourcePort: 55555,
            TargetPort: 22,
            SensorId: "sensor-1",
            Protocol: "ssh",
            SessionId: 1001,
            WasEdgeFiltered: false,
            FilterReason: null,
            Username: "root",
            Password: "toor",
            Command: "whoami",
            Payload: null,
            UserAgent: "attack-client",
            Headers: new Dictionary<string, string> { ["x-trace-id"] = "abc" },
            Geolocation: new AttackGeoLocationDto("Unknown", "XX", "Unknown", null, null, null, null, null),
            RawPayloadJson: "{\"sample\":true}");
    }

    private static Honeypot CreateHoneypot(Guid organizationId, Guid subscriptionId)
    {
        var configResult = HoneypotConfiguration.Create(22);
        Assert.True(configResult.IsSuccess);

        return Honeypot.Reconstruct(
            id: Guid.NewGuid(),
            organizationId: organizationId,
            subscriptionId: subscriptionId,
            name: $"hp-{Guid.NewGuid():N}",
            type: HoneypotType.SSH,
            configuration: configResult.Value,
            deploymentLocation: HoneypotDeploymentLocation.Cloud,
            status: HoneypotStatus.Active,
            externalService: null,
            networkInfo: null,
            health: new HoneypotHealth(status: HoneypotHealthStatus.Healthy),
            statistics: new HoneypotStatistics(),
            createdAt: DateTime.UtcNow.AddDays(-1),
            updatedAt: DateTime.UtcNow);
    }

    private static AttackEvent CreateAttackEvent(Guid honeypotId, Guid organizationId, string externalEventId)
    {
        var createResult = AttackEvent.Create(honeypotId, organizationId, new FakeAttackEventData(externalEventId));
        Assert.True(createResult.IsSuccess);

        return createResult.Value;
    }

    private sealed class FakeAttackEventData : IAttackEventData
    {
        public FakeAttackEventData(string externalEventId)
        {
            ExternalEventId = externalEventId;
        }

        public string ExternalEventId { get; }
        public DateTime Timestamp => DateTime.UtcNow;
        public string EventType => "ssh_brute_force";
        public string Severity => "high";
        public string SourceIP => "1.2.3.4";
        public int SourcePort => 54321;
        public int TargetPort => 22;
        public string? SensorId => "sensor";
        public string? Protocol => "ssh";
        public long SessionId => 1;
        public bool WasEdgeFiltered => false;
        public string? FilterReason => null;
        public string? Username => null;
        public string? Password => null;
        public string? Command => null;
        public byte[]? Payload => null;
        public string? UserAgent => null;
        public IReadOnlyDictionary<string, string>? Headers => null;
        public IGeoLocationData? Geolocation => null;
        public string? RawPayloadJson => "{}";
    }
}
