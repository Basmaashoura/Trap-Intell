using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Organizations.Commands.InviteUser;
using Trap_Intel.Application.Organizations.Commands.AcceptInvitation;
using Trap_Intel.Application.Organizations.Commands.RevokeInvitation;
using Trap_Intel.Application.Organizations.Commands.ResendInvitation;
using Trap_Intel.Application.Organizations.Queries.GetOrganizationInvitations;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Invitations.Enums;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Api.Endpoints.Organizations;

internal sealed class InvitationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organizations")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/{organizationId:guid}/invitations", InviteUser)
            .WithName("InviteUser")
            .WithSummary("Invites a user to the organization")
            .RequirePermission(Permissions.Users.Invite)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{organizationId:guid}/invitations", GetInvitations)
            .WithName("GetOrganizationInvitations")
            .WithSummary("Gets organization invitations")
            .RequirePermission(Permissions.Users.View)
            .Produces<PagedResult<Models.OrganizationInvitationResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/{organizationId:guid}/invitations/{invitationId:guid}/resend", ResendInvitation)
            .WithName("ResendInvitation")
            .WithSummary("Resends an organization invitation")
            .RequirePermission(Permissions.Users.Invite)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{organizationId:guid}/invitations/{invitationId:guid}", RevokeInvitation)
            .WithName("RevokeInvitation")
            .WithSummary("Revokes a pending organization invitation")
            .RequirePermission(Permissions.Users.Invite)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/invitations/accept", AcceptInvitation)
            .WithName("AcceptInvitation")
            .WithSummary("Accepts an organization invitation using a token")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> InviteUser(
        Guid organizationId,
        [FromBody] Models.InviteUserRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var inviterId))
        {
            return Results.Unauthorized();
        }

        var callerRoleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;

        if (!SystemRoles.TryResolveRoleId(callerRoleClaim, out var callerRoleId))
        {
            return Results.Forbid();
        }

        if (!RolePermissionMap.CanAssignRole(callerRoleId, request.RoleId))
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Your role cannot assign the requested role.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var command = new InviteUserCommand(
            organizationId,
            request.Email,
            request.RoleId,
            inviterId,
            request.PersonalMessage,
            request.ExpirationDays ?? 7);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to create invitation",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreateInvitationResponse("User invited successfully.", result.Value, httpContext);
    }

    private static async Task<IResult> GetInvitations(
        Guid organizationId,
        [AsParameters] GlobalListQueryRequest listQuery,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] string? status = null)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        InvitationStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<InvitationStatus>(status, true, out var statusValue))
            {
                return Results.Problem(
                    title: "Invalid invitation status filter",
                    detail: $"Unsupported status '{status}'. Allowed values: Pending, Accepted, Declined, Revoked, Expired.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            parsedStatus = statusValue;
        }

        var query = new GetOrganizationInvitationsQuery(organizationId, parsedStatus, listQuery.ToQueryOptions());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve invitations",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var responseItems = result.Value.Items.Select(invitation => new Models.OrganizationInvitationResponse(
            invitation.Id,
            invitation.OrganizationId,
            invitation.Email,
            invitation.RoleId,
            invitation.InvitedByUserId,
            invitation.Status,
            invitation.PersonalMessage,
            invitation.CreatedAt,
            invitation.ExpiresAt,
            invitation.UpdatedAt,
            invitation.AcceptedAt,
            invitation.DeclinedAt,
            invitation.RevokedAt,
            invitation.IsExpired)).ToList();

        var response = new PagedResult<Models.OrganizationInvitationResponse>(
            responseItems,
            result.Value.PageNumber,
            result.Value.PageSize,
            result.Value.TotalCount);

        var filterKey = listQuery.BuildFilterKey(("status", parsedStatus));
        httpContext.Response.SetListRealtimeHeaders("invitations", "organization", filterKey);

        return Results.Ok(response);
    }

    private static async Task<IResult> ResendInvitation(
        Guid organizationId,
        Guid invitationId,
        [FromBody] Models.ResendInvitationRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var requesterId))
        {
            return Results.Unauthorized();
        }

        var command = new ResendInvitationCommand(
            organizationId,
            invitationId,
            requesterId,
            request?.ExpirationDays ?? 7);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            if (firstError?.Code == "Invitation.NotFound")
            {
                return Results.NotFound(new { message = firstError.Message });
            }

            return Results.Problem(
                title: "Failed to resend invitation",
                detail: firstError?.Message ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreateInvitationResponse("Invitation resent successfully.", result.Value, httpContext);
    }

    private static async Task<IResult> RevokeInvitation(
        Guid organizationId,
        Guid invitationId,
        [FromBody] Models.RevokeInvitationRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var requesterId))
        {
            return Results.Unauthorized();
        }

        var reason = string.IsNullOrWhiteSpace(request?.Reason)
            ? "Revoked by organization administrator"
            : request!.Reason!;

        var command = new RevokeInvitationCommand(
            organizationId,
            invitationId,
            requesterId,
            reason);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            if (firstError?.Code == "Invitation.NotFound")
            {
                return Results.NotFound(new { message = firstError.Message });
            }

            return Results.Problem(
                title: "Failed to revoke invitation",
                detail: firstError?.Message ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invitation revoked successfully." });
    }

    private static async Task<IResult> AcceptInvitation(
        [FromBody] Models.AcceptInvitationRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var acceptingUserId))
        {
            return Results.Unauthorized();
        }

        var command = new AcceptInvitationCommand(request.Token, acceptingUserId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to accept invitation",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Invalid token or user not registered.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invitation accepted. You are now part of the organization." });
    }

    private static IResult CreateInvitationResponse(string message, string rawToken, HttpContext httpContext)
    {
        var hostEnvironment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var allowRawToken = hostEnvironment.IsDevelopment() ||
                            string.Equals(hostEnvironment.EnvironmentName, "Docker", StringComparison.OrdinalIgnoreCase);

        if (!allowRawToken)
        {
            return Results.Ok(new { message });
        }

        return Results.Ok(new { message, rawToken });
    }
}

