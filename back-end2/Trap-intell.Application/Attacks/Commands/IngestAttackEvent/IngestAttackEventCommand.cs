using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Attacks.Commands.IngestAttackEvent;

public sealed record IngestAttackEventCommand(
    Guid OrganizationId,
    Guid HoneypotId,
    string ExternalEventId,
    DateTime Timestamp,
    string EventType,
    string Severity,
    string SourceIP,
    int SourcePort,
    int TargetPort,
    string? SensorId = null,
    string? Protocol = null,
    long SessionId = 0,
    bool WasEdgeFiltered = false,
    string? FilterReason = null,
    string? Username = null,
    string? Password = null,
    string? Command = null,
    byte[]? Payload = null,
    string? UserAgent = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    AttackGeoLocationDto? Geolocation = null,
    string? RawPayloadJson = null
) : IRequest<Result<Guid>>;

public sealed record AttackGeoLocationDto(
    string? Country,
    string? CountryCode,
    string? City,
    decimal? Latitude,
    decimal? Longitude,
    string? Region,
    string? ISP,
    string? ASN);
