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
            .WithTags("Attack Events")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/ingest", Ingest)
            .WithName("IngestAttackEvent")
            .WithSummary("Ingest attack event data from honeypot")
            .WithDescription("Receives and processes attack events from honeypot sensors")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Ingest(
        Guid organizationId,
        Guid honeypotId,
        [FromBody] IngestAttackEventRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation processes and stores attack event data
    }
}
