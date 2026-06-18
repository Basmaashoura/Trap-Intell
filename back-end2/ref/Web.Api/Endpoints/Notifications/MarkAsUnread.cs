using Application.Abstractions.Messaging;
using Application.Notifications.MarkAsUnread;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class MarkAsUnread : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notifications/{id:guid}/unread", async (
            Guid id,
            ICommandHandler<MarkAsUnreadCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new MarkAsUnreadCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Marks a notification as unread.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
