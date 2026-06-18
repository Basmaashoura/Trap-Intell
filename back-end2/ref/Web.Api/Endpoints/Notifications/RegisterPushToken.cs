using Application.Abstractions.Messaging;
using Application.Notifications.RegisterPushToken;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class RegisterPushToken : IEndpoint
{
    public sealed record Request(
        string Token,
        string Platform,
        string? DeviceName);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notifications/push-tokens", async (
            Request request,
            ICommandHandler<RegisterPushTokenCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterPushTokenCommand(
                request.Token,
                request.Platform,
                request.DeviceName);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/notifications/push-tokens/{id}", new { id }),
                CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Register a push notification token");
    }
}
