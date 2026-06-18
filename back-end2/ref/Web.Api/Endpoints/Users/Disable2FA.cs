using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Disable2FA;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for disabling two-factor authentication.
/// </summary>
internal sealed class Disable2FA : IEndpoint
{
    public sealed record Request(string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/disable-2fa", async (
            [FromBody] Request request,
            [FromServices] ICurrentUserService currentUser,
            [FromServices] ICommandHandler<Disable2FACommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new Disable2FACommand(
                currentUser.UserId,
                request.Code);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Two-factor authentication has been disabled." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("Disable2FA")
        .WithSummary("Disable two-factor authentication")
        .WithDescription("Disables 2FA after password verification for security.")
        .Produces(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
