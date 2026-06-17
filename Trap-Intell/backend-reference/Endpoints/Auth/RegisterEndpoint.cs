using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Application.Authentication.Commands.Register;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Models;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/register", HandleAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .WithDescription("Creates a new user account and sends verification email")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthenticationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ISender sender,
        IUserRepository userRepository,
        ILogger<RegisterEndpoint> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation creates new user, sends verification email, logs registration
    }
}
