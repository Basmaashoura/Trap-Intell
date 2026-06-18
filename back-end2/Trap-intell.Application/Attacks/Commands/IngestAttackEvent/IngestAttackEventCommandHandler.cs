using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Attacks.Commands.IngestAttackEvent;

internal sealed class IngestAttackEventCommandHandler : IRequestHandler<IngestAttackEventCommand, Result<Guid>>
{
    private readonly IAttackEventRepository _attackEventRepository;
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IngestAttackEventCommandHandler(
        IAttackEventRepository attackEventRepository,
        IHoneypotRepository honeypotRepository,
        IUnitOfWork unitOfWork)
    {
        _attackEventRepository = attackEventRepository;
        _honeypotRepository = honeypotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(IngestAttackEventCommand request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<Guid>(
                Error.Custom("AttackEvent.InvalidOrganization", "Organization ID cannot be empty."));
        }

        if (request.HoneypotId == Guid.Empty)
        {
            return Result.Failure<Guid>(
                Error.Custom("AttackEvent.InvalidHoneypot", "Honeypot ID cannot be empty."));
        }

        var honeypot = await _honeypotRepository.GetByIdAsync(request.HoneypotId, cancellationToken);
        if (honeypot is null || honeypot.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<Guid>(HoneypotErrors.NotFound);
        }

        var existingEvent = await _attackEventRepository.GetByExternalIdAsync(request.ExternalEventId, cancellationToken);
        if (existingEvent is not null)
        {
            return Result.Success(existingEvent.Id);
        }

        var attackEventData = new AttackEventData(
            request.ExternalEventId,
            request.Timestamp,
            request.EventType,
            request.Severity,
            request.SourceIP,
            request.SourcePort,
            request.TargetPort,
            request.SensorId,
            request.Protocol,
            request.SessionId,
            request.WasEdgeFiltered,
            request.FilterReason,
            request.Username,
            request.Password,
            request.Command,
            request.Payload,
            request.UserAgent,
            request.Headers,
            request.Geolocation,
            request.RawPayloadJson);

        var createResult = AttackEvent.Create(request.HoneypotId, request.OrganizationId, attackEventData);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Errors);
        }

        var attackEvent = createResult.Value;

        await _attackEventRepository.AddAsync(attackEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(attackEvent.Id);
    }

    private sealed record AttackEventData(
        string ExternalEventId,
        DateTime Timestamp,
        string EventType,
        string Severity,
        string SourceIP,
        int SourcePort,
        int TargetPort,
        string? SensorId,
        string? Protocol,
        long SessionId,
        bool WasEdgeFiltered,
        string? FilterReason,
        string? Username,
        string? Password,
        string? Command,
        byte[]? Payload,
        string? UserAgent,
        IReadOnlyDictionary<string, string>? Headers,
        AttackGeoLocationDto? Geolocation,
        string? RawPayloadJson) : IAttackEventData
    {
        IGeoLocationData? IAttackEventData.Geolocation =>
            Geolocation is null
                ? null
                : new GeoLocationData(
                    Geolocation.Country,
                    Geolocation.CountryCode,
                    Geolocation.City,
                    Geolocation.Latitude,
                    Geolocation.Longitude,
                    Geolocation.Region,
                    Geolocation.ISP,
                    Geolocation.ASN);
    }

    private sealed record GeoLocationData(
        string? Country,
        string? CountryCode,
        string? City,
        decimal? Latitude,
        decimal? Longitude,
        string? Region,
        string? ISP,
        string? ASN) : IGeoLocationData;
}
