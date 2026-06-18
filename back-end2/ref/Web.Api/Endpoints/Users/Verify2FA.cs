using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Verify2FA;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for verifying two-factor authentication setup.
/// </summary>
internal sealed class Verify2FA : IEndpoint
{
    public sealed record Request(string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/verify-2fa", async (
            [FromBody] Request request,
            [FromServices] ICurrentUserService currentUser,
            [FromServices] ICommandHandler<Verify2FACommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new Verify2FACommand(
                currentUser.UserId,
                request.Code);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Two-factor authentication has been successfully verified and activated." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("Verify2FA")
        .WithSummary("Verify 2FA setup")
        .WithDescription("Verifies the 2FA code to complete the setup process.")
        .Produces(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
