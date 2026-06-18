using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Application.Authentication.Commands.Register;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Auth;

/// <summary>
/// Endpoint for user registration.
/// </summary>
internal sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/register", HandleAsync)
            .WithName("Register")
            .WithSummary("Register a new user from an invitation")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<RegistrationSuccessResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ISender sender,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<RegisterEndpoint> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.InvitationToken);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Registration Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Could not register user",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            logger.LogError("Registered user not found after successful registration. Email: {Email}", request.Email);
            return Results.Problem(
                title: "Registration Failed",
                detail: "Registered user could not be resolved.",
                statusCode: StatusCodes.Status500InternalServerError,
                instance: httpContext.Request.Path);
        }

        var resolvedRoleName = (await roleRepository.GetByIdAsync(user.RoleId, cancellationToken))?.Name
            ?? SystemRoles.GetName(user.RoleId);

        return Results.Ok(new RegistrationSuccessResponse
        {
            Message = "Registration successful.",
            User = new RegisteredUserResponse
            {
                Id = user.Id,
                Email = user.Email.Value,
                UserName = user.UserName.Value,
                FirstName = user.FirstName.Value,
                LastName = user.LastName.Value,
                FullName = user.FullName,
                RoleId = user.RoleId,
                Role = resolvedRoleName,
                OrganizationId = user.OrganizationId
            }
        });
    }
}

internal sealed record RegistrationSuccessResponse
{
    public required string Message { get; init; }
    public required RegisteredUserResponse User { get; init; }
}

internal sealed record RegisteredUserResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required Guid RoleId { get; init; }
    public required string Role { get; init; }
    public required Guid OrganizationId { get; init; }
}
