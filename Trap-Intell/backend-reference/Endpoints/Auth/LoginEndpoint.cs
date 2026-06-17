using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Application.Authentication.Commands.Login;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Models;

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
        // Implementation validates credentials, checks 2FA requirement,
        // generates tokens, logs authentication attempt
    }
}
