using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Organizations.Commands.CreateOrganization;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Organizations;

internal sealed class CreateTenantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organizations")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/", HandleAsync)
            .WithName("CreateOrganization")
            .WithSummary("Creates a new organization / tenant")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateOrganizationCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Organization Creation Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/organizations/{result.Value}", result.Value);
    }
}
