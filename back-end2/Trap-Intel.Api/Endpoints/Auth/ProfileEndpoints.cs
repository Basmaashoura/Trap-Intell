using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Users.Commands.UpdateCurrentUserProfile;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class ProfileEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user information")
            .RequireAuthorization()
            .Produces<UserInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me/profile", UpdateCurrentUserProfile)
            .WithName("UpdateCurrentUserProfile")
            .WithSummary("Update current user's profile")
            .WithDescription("Updates the profile information for the currently authenticated user.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetCurrentUser(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return Results.Unauthorized();
        }

        var resolvedRoleName = (await roleRepository.GetByIdAsync(user.RoleId, cancellationToken))?.Name
            ?? Trap_Intel.Domain.Roles.SystemRoles.GetName(user.RoleId);

        var userInfo = new UserInfo
        {
            Id = user.Id,
            Email = user.Email.Value,
            UserName = user.UserName.Value,
            FirstName = user.FirstName.Value,
            LastName = user.LastName.Value,
            FullName = $"{user.FirstName.Value} {user.LastName.Value}",
            RoleId = user.RoleId,
            Role = resolvedRoleName,
            OrganizationId = user.OrganizationId,
            EmailConfirmed = user.Status != UserStatus.PendingActivation,
            TwoFactorEnabled = false,
            Permissions = user.GetPermissions()
        };

        return Results.Ok(userInfo);
    }

    private static async Task<IResult> UpdateCurrentUserProfile(
        [FromBody] UpdateProfileRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateCurrentUserProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.JobTitle,
            request.Department,
            request.Location,
            request.Bio,
            request.WebsiteUrl,
            request.LinkedInUrl,
            request.GitHubUrl,
            request.XUrl);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to update profile",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Profile updated successfully." });
    }
}

public sealed record UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(120)]
    public string? JobTitle { get; init; }

    [MaxLength(120)]
    public string? Department { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }

    [MaxLength(2000)]
    public string? Bio { get; init; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; init; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; init; }

    [MaxLength(500)]
    public string? GitHubUrl { get; init; }

    [MaxLength(500)]
    public string? XUrl { get; init; }
}

