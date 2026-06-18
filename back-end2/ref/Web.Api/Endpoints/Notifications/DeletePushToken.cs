using Application.Abstractions.Messaging;
using Application.Notifications.DeletePushToken;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class DeletePushToken : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("notifications/push-tokens/{id:guid}", async (
            Guid id,
            ICommandHandler<DeletePushTokenCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeletePushTokenCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Deletes a push token by ID.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
