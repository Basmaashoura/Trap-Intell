using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Endpoints;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Attacks.Commands.IngestAttackEvent;

namespace Trap_Intel.Api.Endpoints.Attacks;

internal sealed class AttackEventIngestionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/honeypots/{honeypotId:guid}/attacks")
            .WithTags("Attacks")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/events", Ingest)
            .WithName("IngestAttackEvent")
            .WithSummary("Ingests a honeypot attack event")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> Ingest(
        Guid organizationId,
        Guid honeypotId,
        [FromBody] IngestAttackEventRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
        {
            return Results.Forbid();
        }

        var command = new IngestAttackEventCommand(
            organizationId,
            honeypotId,
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
            request.Geolocation is null
                ? null
                : new AttackGeoLocationDto(
                    request.Geolocation.Country,
                    request.Geolocation.CountryCode,
                    request.Geolocation.City,
                    request.Geolocation.Latitude,
                    request.Geolocation.Longitude,
                    request.Geolocation.Region,
                    request.Geolocation.ISP,
                    request.Geolocation.ASN),
            request.RawPayloadJson);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Honeypot.NotFound"
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(
                    title: "Attack event ingestion failed",
                    detail: error?.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        }

        var attackEventId = result.Value;
        var location = $"/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks/events/{attackEventId}";
        return Results.Created(location, new { attackEventId });
    }
}

public sealed record IngestAttackEventRequest(
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
    Dictionary<string, string>? Headers = null,
    IngestAttackGeoLocationRequest? Geolocation = null,
    string? RawPayloadJson = null);

public sealed record IngestAttackGeoLocationRequest(
    string? Country,
    string? CountryCode,
    string? City,
    decimal? Latitude,
    decimal? Longitude,
    string? Region,
    string? ISP,
    string? ASN);
