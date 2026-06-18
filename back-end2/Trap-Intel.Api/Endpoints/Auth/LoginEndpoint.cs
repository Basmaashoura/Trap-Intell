using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Application.Authentication.Commands.Login;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Api.Endpoints.Auth.Models;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Auth;

/// <summary>
/// Endpoint for login authentication.
/// </summary>
internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/login", HandleAsync)
            .WithName("Login")
            .WithSummary("Authenticate with email and password")
            .WithDescription("Rate limited to prevent brute force attacks. Logs all attempts for security auditing.")
            .AllowAnonymous()
            .RequireRateLimiting("auth") // Strict rate limiting for login
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces<TwoFactorRequiredResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        ISender sender,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<LoginEndpoint> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ipAddress = AuthHelpers.GetClientIpAddress(httpContext);
        var userAgent = AuthHelpers.GetUserAgent(httpContext);

        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.RememberMe,
            ipAddress,
            userAgent);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            // Handle domain mapped errors directly
            if (error?.Code == "Identity.TwoFactorRequired" && error.Data != null)
            {
                var twoFactorToken = error.Data.TryGetValue("TwoFactorToken", out var token) ? token?.ToString() : "";
                var userId = error.Data.TryGetValue("UserId", out var uid) && uid is Guid guid ? guid : Guid.Empty;

                return Results.Ok(new TwoFactorRequiredResponse
                {
                    TwoFactorToken = twoFactorToken ?? "",
                    UserId = userId,
                    Message = "Two-factor authentication code required."
                });
            }

            return Results.Problem(
                title: "Authentication Failed",
                detail: error?.Message ?? "Invalid credentials",
                statusCode: StatusCodes.Status401Unauthorized,
                instance: httpContext.Request.Path);
        }

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            logger.LogError("Authenticated user not found after successful login. Email: {Email}", request.Email);
            return Results.Problem(
                title: "Authentication Failed",
                detail: "Authenticated user could not be resolved.",
                statusCode: StatusCodes.Status500InternalServerError,
                instance: httpContext.Request.Path);
        }

        var resolvedRoleName = (await roleRepository.GetByIdAsync(user.RoleId, cancellationToken))?.Name
            ?? SystemRoles.GetName(user.RoleId);

        var response = new AuthenticationResponse
        {
            AccessToken = result.Value.AccessToken,
            RefreshToken = result.Value.RefreshToken,
            ExpiresIn = result.Value.ExpiresIn,
            RefreshTokenExpiresAt = result.Value.RefreshTokenExpiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email.Value,
                UserName = user.UserName.Value,
                FirstName = user.FirstName.Value,
                LastName = user.LastName.Value,
                FullName = user.FullName,
                RoleId = user.RoleId,
                Role = resolvedRoleName,
                OrganizationId = user.OrganizationId,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Permissions = user.GetPermissions()
            }
        };

        return Results.Ok(response);
    }
}
