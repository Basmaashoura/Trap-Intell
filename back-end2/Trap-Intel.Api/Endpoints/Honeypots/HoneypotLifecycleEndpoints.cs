using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Application.Honeypots.Commands.DeployHoneypot;
using Trap_Intel.Application.Honeypots.Commands.PauseHoneypot;
using Trap_Intel.Application.Honeypots.Commands.ResumeHoneypot;
using Trap_Intel.Application.Honeypots.Commands.TerminateHoneypot;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Honeypots;

internal sealed class HoneypotLifecycleEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/honeypots")
            .WithTags("Honeypots")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization(); // Requires OrgAdmin Policy natively

        group.MapPost("/", Deploy)
            .WithName("DeployHoneypot")
            .WithSummary("Registers and initiates a honeypot configuration deployment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{honeypotId:guid}/pause", Pause)
            .WithName("PauseHoneypot")
            .WithSummary("Temporarily suspends an active honeypot")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{honeypotId:guid}/resume", Resume)
            .WithName("ResumeHoneypot")
            .WithSummary("Resumes a previously paused honeypot")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{honeypotId:guid}/terminate", Terminate)
            .WithName("TerminateHoneypot")
            .WithSummary("Permanently kills the honeypot and removes it from active billing")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Deploy(
        Guid organizationId,
        [FromBody] DeployHoneypotRequest request,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Enforce Orga Boundary
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new DeployHoneypotCommand(
            organizationId,
            request.SubscriptionId,
            request.Name,
            request.Type,
            request.Location,
            request.ConfigTemplateBase64
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Deployment Initialization Failed", detail: result.Errors.FirstOrDefault()?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Honeypot deployment processing." });
    }

    private static async Task<IResult> Pause(
        Guid organizationId,
        Guid honeypotId,
        [FromQuery] string? reason,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new PauseHoneypotCommand(organizationId, honeypotId, reason);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Honeypot.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Pause Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Honeypot paused successfully." });
    }

    private static async Task<IResult> Resume(
        Guid organizationId,
        Guid honeypotId,
        [FromQuery] string? reason,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new ResumeHoneypotCommand(organizationId, honeypotId, reason);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Honeypot.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Resume Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Honeypot resumed successfully." });
    }

    private static async Task<IResult> Terminate(
        Guid organizationId,
        Guid honeypotId,
        [FromQuery] string? reason,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new TerminateHoneypotCommand(organizationId, honeypotId, reason);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Honeypot.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Termination Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Honeypot terminated safely." });
    }
}

public sealed record DeployHoneypotRequest(
    Guid SubscriptionId,
    string Name,
    HoneypotType Type,
    HoneypotDeploymentLocation Location,
    string ConfigTemplateBase64
);
