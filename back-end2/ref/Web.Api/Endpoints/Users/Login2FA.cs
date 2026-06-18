using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Login2FA;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for completing login with 2FA code.
/// Used when initial login returns TwoFactorRequired error.
/// </summary>
internal sealed class Login2FA : IEndpoint
{
    public sealed record Request(string Email, string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login-2fa", async (
            Request request,
            ICommandHandler<Login2FACommand, TokenResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new Login2FACommand(request.Email, request.Code);

            Result<TokenResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                tokens => Results.Ok(tokens),
                CustomResults.Problem);
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.Authentication)
        .WithTags(Tags.Users)
        .WithName("Login2FA")
        .WithSummary("Complete login with 2FA code")
        .WithDescription("Completes the login process for users with 2FA enabled. Use the 6-digit code from your authenticator app.")
        .Produces<TokenResponse>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}
