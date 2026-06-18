using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Infrastructure.Authentication.Services;
using Trap_Intel.Api.Endpoints.Auth.Models;
using Trap_Intel.Api.Filters;
using System.Security.Claims;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class TokenEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token using refresh token")
            .WithDescription("Implements token rotation - old token is invalidated and new token is returned")
            .AllowAnonymous()
            .RequireRateLimiting("auth") // Rate limit token refresh
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden); // For reuse detection

        // Protected endpoints
        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Revoke refresh token and logout")
            .WithDescription("Optionally pass refreshToken to revoke specific session, or logoutAll=true to revoke all sessions")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/logout-all", LogoutAll)
            .WithName("LogoutAll")
            .WithSummary("Revoke all refresh tokens and logout from all devices")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK);

        group.MapGet("/sessions", GetActiveSessions)
            .WithName("GetActiveSessions")
            .WithSummary("Get count of active sessions for current user")
            .RequireAuthorization()
            .Produces<ActiveSessionsResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> RefreshToken(
        RefreshTokenRequest request,
        IAuthenticationService authService,
        ILogger<TokenEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ipAddress = AuthHelpers.GetClientIpAddress(httpContext);
        var userAgent = AuthHelpers.GetUserAgent(httpContext);

        logger.LogInformation("Token refresh attempt from IP: {IpAddress}", ipAddress);

        var result = await authService.RefreshTokenAsync(
            request.RefreshToken,
            ipAddress,
            userAgent,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            // Check if it's a token reuse error (security event)
            if (error?.Code == "Identity.RefreshTokenReused")
            {
                logger.LogWarning("SECURITY ALERT: Refresh token reuse detected from IP: {IpAddress}", ipAddress);
                return Results.Problem(
                    title: "Token Compromised",
                    detail: error.Message,
                    statusCode: StatusCodes.Status403Forbidden,
                    instance: httpContext.Request.Path);
            }

            return Results.Problem(
                title: "Token Refresh Failed",
                detail: error?.Message ?? "Invalid refresh token",
                statusCode: StatusCodes.Status401Unauthorized,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Logout(
        [FromBody] LogoutRequest request,
        IAuthenticationService authService,
        ILogger<TokenEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        if (request.LogoutAll)
        {
            logger.LogInformation("User {UserId} requested logout from all devices", userId);
            await authService.LogoutAllAsync(userId, cancellationToken);
            return Results.Ok(new { message = "Logged out from all devices successfully." });
        }

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogInformation("User {UserId} logged out from specific session", userId);
            await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        }
        else
        {
            logger.LogInformation("User {UserId} logged out without providing refresh token", userId);
        }

        return Results.Ok(new { message = "Logged out successfully." });
    }

    private static async Task<IResult> LogoutAll(
        IAuthenticationService authService,
        ILogger<TokenEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        logger.LogInformation("User {UserId} requested logout from all devices (LogoutAll endpoint)", userId);
        var result = await authService.LogoutAllAsync(userId, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Logout Failed",
                detail: "Failed to revoke all sessions",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new { message = $"Logged out from all {result.Value} devices successfully." });
    }

    private static async Task<IResult> GetActiveSessions(
        IRefreshTokenService refreshTokenService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await refreshTokenService.GetActiveSessionCountAsync(userId, cancellationToken);

        return Results.Ok(new ActiveSessionsResponse { Count = result, UserId = userId });
    }
}
